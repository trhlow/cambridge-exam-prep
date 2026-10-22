// VE-41: load test of one exam room. N candidates sign in, start the first section, keep the candidate hub open,
// send the heartbeat every 5 s and save an answer every few seconds, as the browser does; an invigilator watches the
// section on the staff hub, reloads the monitor and pauses and resumes some candidates in the middle of the run.
// Every request is timed; the report gives percentiles per kind of request, errors, how fast a pause reaches the
// candidates' screens, and the containers' CPU and memory.
//
//   cd setup/loadtest && npm install
//   node loadtest.mjs --base http://localhost:8090 --candidates 100 --minutes 5
//
// Run it against the isolated e2e stack (docker/docker-compose.e2e.yml, port 8090), never the demo database: it
// creates an exam session "Load test …" with its candidates.

import { HttpTransportType, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { execFile } from 'node:child_process'
import { mkdir, writeFile } from 'node:fs/promises'
import { promisify } from 'node:util'

const options = parseArgs(process.argv.slice(2), {
  base: 'http://localhost:8090',
  candidates: '100',
  minutes: '5',
  ramp: '30',
  admin: 'hoanglong:19072005',
  invigilator: 'giamthi:victory-invig',
  pausing: '10',
  out: 'results',
})
const BASE = options.base.replace(/\/$/, '')
const CANDIDATES = Number(options.candidates)
const RUN_MS = Number(options.minutes) * 60_000
const RAMP_MS = Number(options.ramp) * 1000
const HEARTBEAT_MS = 5_000
const SAVE_MS = [4_000, 12_000]

function parseArgs(args, defaults) {
  const result = { ...defaults }
  for (let i = 0; i < args.length; i += 2) {
    result[args[i].replace(/^--/, '')] = args[i + 1]
  }
  return result
}

// A connection that fails for good (e.g. the server refusing it under load) is counted, not fatal to the run.
let lostConnections = 0
process.on('unhandledRejection', () => lostConnections++)

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms))
const random = (min, max) => min + Math.random() * (max - min)

// ---------------------------------------------------------------- timing

const timings = new Map() // kind → [ms]
const failures = new Map() // kind → { status → count }

function record(kind, ms, status) {
  if (status >= 200 && status < 300) {
    if (!timings.has(kind)) timings.set(kind, [])
    timings.get(kind).push(ms)
  } else {
    if (!failures.has(kind)) failures.set(kind, {})
    const byStatus = failures.get(kind)
    byStatus[status] = (byStatus[status] ?? 0) + 1
  }
}

function percentile(sorted, p) {
  if (sorted.length === 0) return NaN
  return sorted[Math.min(sorted.length - 1, Math.floor((p / 100) * sorted.length))]
}

// ---------------------------------------------------------------- HTTP with a cookie jar per user

class Client {
  constructor() {
    this.cookies = new Map()
  }

  cookieHeader() {
    return [...this.cookies].map(([name, value]) => `${name}=${value}`).join('; ')
  }

  async send(kind, method, path, body) {
    const started = performance.now()
    let status = 0
    try {
      const response = await fetch(BASE + path, {
        method,
        headers: { 'Content-Type': 'application/json', Accept: 'application/json', Cookie: this.cookieHeader(), 'X-Client-Time': String(Date.now()) },
        body: body === undefined ? undefined : JSON.stringify(body),
      })
      status = response.status
      for (const cookie of response.headers.getSetCookie()) {
        const [pair] = cookie.split(';')
        const index = pair.indexOf('=')
        this.cookies.set(pair.slice(0, index), pair.slice(index + 1))
      }
      const text = await response.text()
      record(kind, performance.now() - started, status)
      if (status >= 400) {
        throw new Error(`${method} ${path} → ${status} ${text.slice(0, 200)}`)
      }
      return text ? JSON.parse(text) : null
    } catch (error) {
      if (status === 0) record(kind, performance.now() - started, 0)
      throw error
    }
  }

  hub(path) {
    return new HubConnectionBuilder()
      .withUrl(BASE + path, { transport: HttpTransportType.WebSockets, skipNegotiation: true, headers: { Cookie: this.cookieHeader() } })
      .withAutomaticReconnect()
      // As the browser does (src/frontend/src/api/live.ts): the API drops a connection silent for 12 s.
      .withKeepAliveInterval(4_000)
      .withServerTimeout(12_000)
      .configureLogging(LogLevel.None)
      .build()
  }
}

async function signInStaff(account) {
  const [username, password] = account.split(':')
  const client = new Client()
  await client.send('staff login', 'POST', '/api/staff/auth/login', { username, password })
  return client
}

// ---------------------------------------------------------------- the exam session

async function createSession(admin) {
  const types = await admin.send('setup', 'GET', '/api/admin/exam-types')
  const a2 = types.find((t) => t.code === 'A2_KEY_FS')
  const papers = await admin.send('setup', 'GET', '/api/admin/exam-papers')
  const sectionPapers = a2.sections.map((section) => {
    const paper = papers.find((p) => p.sectionTemplateId === section.id && p.status === 'Published' && !p.paperMixId)
    if (!paper) throw new Error(`No published paper for ${section.title}.`)
    return { sectionTemplateId: section.id, testPaperId: paper.id }
  })
  const name = `Load test ${new Date().toISOString().slice(0, 16).replace('T', ' ')} (${CANDIDATES} candidates)`
  const exam = await admin.send('setup', 'POST', '/api/admin/exams', {
    name,
    examTypeId: a2.id,
    scheduledDate: new Date().toISOString().slice(0, 10),
    room: 'Load test',
    papers: sectionPapers,
    sessionPin: '4821',
  })
  await admin.send('setup', 'POST', `/api/admin/exams/${exam.id}/status`, { status: 'Active' })
  const staff = await admin.send('setup', 'GET', '/api/admin/assignable-staff')
  const invigilator = staff.find((s) => s.username === options.invigilator.split(':')[0])
  await admin.send('setup', 'PUT', `/api/admin/exams/${exam.id}/invigilators`, { userIds: [invigilator.id] })

  const rows = Array.from({ length: CANDIDATES }, (_, i) => ({
    candidateNumber: String(i + 1).padStart(4, '0'),
    firstName: `Load${i + 1}`,
    lastName: 'Tester',
    seatNo: String(i + 1),
  }))
  for (let i = 0; i < rows.length; i += 500) {
    await admin.send('setup', 'POST', `/api/admin/exams/${exam.id}/candidates`, { candidates: rows.slice(i, i + 500) })
  }

  const slips = await admin.send('setup', 'GET', `/api/staff/exams/${exam.id}/login-slips`)
  return { examId: exam.id, name, slips }
}

// ---------------------------------------------------------------- one candidate

function answersFor(paper) {
  const answers = []
  for (const part of paper.parts) {
    for (const group of part.groups) {
      for (const question of group.questions) {
        const keys = (question.options ?? group.optionPool ?? []).map((o) => o.key)
        const value =
          group.interactionType === 'Essay'
            ? 'Dear Jo, I would like to go to the cinema with you next Saturday afternoon. '.repeat(3)
            : keys.length > 0
              ? keys[Math.floor(Math.random() * keys.length)]
              : 'answer'
        answers.push({ questionNumber: question.number, value })
      }
    }
  }
  return answers
}

class Candidate {
  constructor(slip) {
    this.slip = slip
    this.client = new Client()
    this.refreshes = []
    this.reconnects = 0
    this.online = false
  }

  async start() {
    await this.client.send('candidate login', 'POST', '/api/candidate/auth/login', { username: this.slip.username, password: this.slip.password })
    await this.client.send('candidate PIN', 'POST', '/api/candidate/auth/pin', { pin: '4821' })
    const sections = await this.client.send('sections', 'GET', '/api/candidate/sections')
    this.sectionId = sections[0].id
    await this.client.send('start attempt', 'POST', `/api/candidate/sections/${this.sectionId}/attempt`)
    const paper = await this.client.send('paper', 'GET', `/api/candidate/sections/${this.sectionId}/paper`)
    this.answers = answersFor(paper)

    this.hub = this.client.hub('/hubs/candidate')
    this.hub.on('refresh', () => this.refreshes.push(performance.now()))
    this.hub.onclose(() => (this.online = false))
    this.hub.onreconnecting(() => this.reconnects++)
    await this.hub.start()
    this.online = true
  }

  async run(until) {
    let nextHeartbeat = performance.now()
    let nextSave = performance.now() + random(...SAVE_MS)
    let index = 0
    while (performance.now() < until) {
      const now = performance.now()
      try {
        if (now >= nextHeartbeat) {
          nextHeartbeat += HEARTBEAT_MS
          await this.client.send('heartbeat', 'POST', `/api/candidate/sections/${this.sectionId}/heartbeat`)
        }

        if (now >= nextSave) {
          nextSave = now + random(...SAVE_MS)
          const answer = this.answers[index++ % this.answers.length]
          // 409 while paused is the API refusing a save, as on a real screen; it is counted, not retried.
          await this.client.send('save answer', 'PUT', `/api/candidate/sections/${this.sectionId}/responses`, { responses: [answer] }).catch(() => {})
        }
      } catch {
        // Counted in the failures table.
      }

      await sleep(Math.max(10, Math.min(nextHeartbeat, nextSave) - performance.now()))
    }
  }

  async stop() {
    await this.hub?.stop().catch(() => {})
  }
}

// ---------------------------------------------------------------- the invigilator

async function invigilate(session, candidates, sectionId, until) {
  const staff = await signInStaff(options.invigilator)
  const hub = staff.hub('/hubs/staff')
  let changes = 0
  let reconnects = 0
  hub.on('sectionChanged', () => changes++)
  hub.onreconnecting(() => reconnects++)
  // A reconnected connection is a new one: it has to join the section's group again, as the monitoring page does.
  hub.onreconnected(() => hub.invoke('WatchSection', session.examId, sectionId).catch(() => {}))
  await hub.start()
  await hub.invoke('WatchSection', session.examId, sectionId)

  const monitorPath = `/api/staff/exams/${session.examId}/sections/${sectionId}/monitor`
  const pauses = []
  const halfway = performance.now() + (until - performance.now()) / 2
  let paused = false
  let lastOnline = 0
  while (performance.now() < until) {
    const monitor = await staff.send('monitor', 'GET', monitorPath).catch(() => null)
    if (monitor) lastOnline = monitor.candidates.filter((c) => c.isOnline ?? c.online).length

    if (!paused && performance.now() >= halfway) {
      paused = true
      const chosen = candidates.filter((c) => c.online).slice(0, Number(options.pausing))
      const ids = chosen.map((c) => c.slip.candidateId)
      for (const action of ['Pause', 'Resume']) {
        const before = chosen.map((c) => c.refreshes.length)
        const sent = performance.now()
        await staff.send(`action ${action}`, 'POST', `/api/staff/exams/${session.examId}/sections/${sectionId}/actions`, { action, candidateIds: ids })
        await sleep(3_000)
        chosen.forEach((c, i) => {
          const arrived = c.refreshes.slice(before[i])[0]
          pauses.push({ action, ms: arrived === undefined ? null : arrived - sent })
        })
      }
    }

    await sleep(3_000)
  }

  await hub.stop()
  return { changes, reconnects, pauses, lastOnline }
}

// ---------------------------------------------------------------- container usage

const exec = promisify(execFile)

async function dockerStats() {
  try {
    const { stdout } = await exec('docker', ['stats', '--no-stream', '--format', '{{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}'])
    return stdout.trim().split('\n').filter((line) => line.includes('api') || line.includes('mysql') || line.includes('web'))
  } catch {
    return []
  }
}

// ---------------------------------------------------------------- the run

const admin = await signInStaff(options.admin)
console.log(`Creating a session with ${CANDIDATES} candidates on ${BASE}…`)
const session = await createSession(admin)
const candidates = session.slips.map((slip) => new Candidate(slip))

console.log(`Signing in over ${RAMP_MS / 1000} s…`)
const startFailures = []
await Promise.all(
  candidates.map(async (candidate, i) => {
    await sleep((RAMP_MS * i) / candidates.length)
    await candidate.start().catch((error) => startFailures.push(String(error.message ?? error)))
  }),
)
const running = candidates.filter((c) => c.online)
console.log(`${running.length} of ${candidates.length} candidates working; running for ${RUN_MS / 60_000} min…`)

const until = performance.now() + RUN_MS
const statsDuring = []
const statsTimer = setInterval(async () => statsDuring.push(...(await dockerStats())), 30_000)
const [watch] = await Promise.all([invigilate(session, running, running[0]?.sectionId, until), ...running.map((c) => c.run(until))])
clearInterval(statsTimer)
const connectedAtEnd = running.filter((c) => c.online).length
await Promise.all(candidates.map((c) => c.stop()))

// ---------------------------------------------------------------- report

const lines = []
const say = (line = '') => {
  lines.push(line)
  console.log(line)
}

const totalRequests = [...timings.values()].reduce((n, list) => n + list.length, 0) + [...failures.values()].reduce((n, by) => n + Object.values(by).reduce((a, b) => a + b, 0), 0)
say(`# Kiểm thử tải — ${session.name}`)
say()
say(`- Máy chủ: ${BASE}; ${CANDIDATES} thí sinh, đăng nhập rải trong ${RAMP_MS / 1000} s, làm bài ${RUN_MS / 60_000} phút`)
say(`- Mỗi thí sinh: heartbeat 5 s/lần, lưu một câu trả lời mỗi ${SAVE_MS[0] / 1000}–${SAVE_MS[1] / 1000} s, giữ kết nối SignalR /hubs/candidate`)
say(`- Giám thị: theo dõi qua /hubs/staff, tải lại trang giám sát mỗi 3 s; giữa giờ tạm dừng rồi cho làm tiếp ${options.pausing} thí sinh`)
say(`- Thí sinh vào được bài: ${running.length}/${CANDIDATES}; còn kết nối SignalR lúc kết thúc: ${connectedAtEnd}/${running.length}; số lần kết nối lại của thí sinh: ${running.reduce((n, c) => n + c.reconnects, 0)}; trang giám sát thấy online lần cuối: ${watch.lastOnline}`)
say(`- Tổng số yêu cầu: ${totalRequests}; sự kiện sectionChanged giám thị nhận: ${watch.changes}; số lần kết nối lại của giám thị: ${watch.reconnects}`)
say(`- Kết nối SignalR hỏng hẳn (không nối lại được): ${lostConnections}`)
say()
say('| Loại yêu cầu | Số lần | p50 (ms) | p95 (ms) | p99 (ms) | Lớn nhất (ms) | Lỗi |')
say('|---|---:|---:|---:|---:|---:|---|')
for (const kind of new Set([...timings.keys(), ...failures.keys()])) {
  const sorted = [...(timings.get(kind) ?? [])].sort((a, b) => a - b)
  const errors = Object.entries(failures.get(kind) ?? {}).map(([status, n]) => `${n}× ${status === '0' ? 'mạng' : status}`).join(', ') || '0'
  const f = (v) => (Number.isNaN(v) ? '–' : Math.round(v))
  say(`| ${kind} | ${sorted.length} | ${f(percentile(sorted, 50))} | ${f(percentile(sorted, 95))} | ${f(percentile(sorted, 99))} | ${f(sorted.at(-1) ?? NaN)} | ${errors} |`)
}
say()
for (const action of ['Pause', 'Resume']) {
  const arrived = watch.pauses.filter((p) => p.action === action && p.ms !== null).map((p) => p.ms).sort((a, b) => a - b)
  const total = watch.pauses.filter((p) => p.action === action).length
  say(`- ${action}: ${arrived.length}/${total} màn hình thí sinh nhận lệnh qua SignalR; p50 ${Math.round(percentile(arrived, 50))} ms, lớn nhất ${Math.round(arrived.at(-1) ?? NaN)} ms`)
}
if (startFailures.length > 0) {
  say()
  say(`Thí sinh không vào được bài (${startFailures.length}):`)
  for (const failure of [...new Set(startFailures)].slice(0, 10)) say(`- ${failure}`)
}
say()
say('Tài nguyên container (docker stats, mỗi 30 s):')
say()
say('```')
for (const line of statsDuring) say(line)
say('```')

await mkdir(options.out, { recursive: true })
const file = `${options.out}/${new Date().toISOString().slice(0, 16).replace(/[:T]/g, '-')}-${CANDIDATES}.md`
await writeFile(file, lines.join('\n') + '\n', 'utf8')
console.log(`\nReport written to ${file}`)
process.exit(0)
