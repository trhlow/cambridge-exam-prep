import { expect, test, type BrowserContext, type Locator, type Page } from '@playwright/test'
import fs from 'node:fs'
import path from 'node:path'

/**
 * VE-42: the mock exam of setup/mock-exam/KICH_BAN_THI_THU.md, played by browsers. An administrator prepares an A2
 * exam (seven candidates, one with 25 % extra time, one who never comes) and a B1 exam through the staff portal;
 * six candidates, the invigilator, the examiner and a second administrator then go through the 16 situations.
 * A situation that fails is written down and the run goes on, as at a real mock exam. The minutes (ket-qua.json and
 * screenshots) go to setup/mock-exam/results/<date>/; make_minutes.py turns them into the Word minutes.
 */

const admin = { username: 'hoanglong', password: '19072005' }
const secondAdmin = { username: 'admin2', password: '19072005' }
const invigilator = { username: 'giamthi', password: 'victory-invig' }
const examiner = { username: 'giamkhao', password: '19072005' }

const A2_PIN = '2468'
const B1_PIN = '1357'
const LISTENING_MINUTES = 30

const day = new Date().toLocaleDateString('sv-SE', { timeZone: 'Asia/Ho_Chi_Minh' })
const OUT = process.env.MOCK_EXAM_OUT ?? path.resolve(process.cwd(), '../../setup/mock-exam/results', day)

type Person = { number: string; family: string; given: string; extra: number; seat: number }
const A2_PEOPLE: Person[] = [
  { number: '0101', family: 'Nguyễn', given: 'An', extra: 0, seat: 1 },
  { number: '0102', family: 'Lê', given: 'Thị Hoa', extra: 25, seat: 2 },
  { number: '0103', family: 'Trần', given: 'Bình', extra: 0, seat: 3 },
  { number: '0104', family: 'Phạm', given: 'Chi', extra: 0, seat: 4 },
  { number: '0105', family: 'Võ', given: 'Dung', extra: 0, seat: 5 },
  { number: '0106', family: 'Đỗ', given: 'Giang', extra: 0, seat: 6 },
  { number: '0107', family: 'Hồ', given: 'Khoa', extra: 0, seat: 7 },
]
const B1_PEOPLE: Person[] = [
  { number: '0201', family: 'Ngô', given: 'Hạnh', extra: 0, seat: 1 },
  { number: '0202', family: 'Lý', given: 'Minh', extra: 0, seat: 2 },
]
const fullName = (p: Person) => `${p.given} ${p.family}`

type Slip = { number: string; name: string; username: string; password: string }
type Candidate = { person: Person; name: string; slip: Slip; context: BrowserContext; page: Page }
type MonitorRow = {
  candidateId: number
  fullName: string
  status: string
  isOnline: boolean
  endsAt: string | null
  remainingSeconds: number | null
  plannedExtraSeconds: number
  incidentExtraSeconds: number
  endTimeEdited: boolean
  submitReason: string | null
  audioPositionSeconds: number | null
  answered: number
  warnings: number
}
type SectionResult = { sectionId: number; title: string; status: string; rawScore: number | null; maxScore: number | null; gradingStatus: string; attemptId: number | null }
type CandidateResult = { candidateId: number; fullName: string; sections: SectionResult[]; markingCode?: string | null }

type Situation = { no: number; title: string; passed: boolean; notes: string[]; error?: string; screenshots: string[] }
const situations: Situation[] = []
const log: string[] = []
const startedAt = new Date().toISOString()

const clock = () => new Date().toLocaleTimeString('en-GB', { timeZone: 'Asia/Ho_Chi_Minh' })
function note(text: string) {
  log.push(`${clock()} ${text}`)
  console.log(`${clock()} ${text}`)
}

function save() {
  const ordered = [...situations].sort((a, b) => a.no - b.no)
  fs.writeFileSync(path.join(OUT, 'ket-qua.json'), JSON.stringify({ startedAt, savedAt: new Date().toISOString(), situations: ordered, log }, null, 2))
}

/** One row of the minutes: runs the situation, writes down what was seen, and goes on if it fails. */
async function situation(no: number, title: string, body: (s: Situation) => Promise<void>) {
  const s: Situation = { no, title, passed: true, notes: [], screenshots: [] }
  situations.push(s)
  note(`#${no} ${title}`)
  await test.step(`${no}. ${title}`, async () => {
    try {
      await body(s)
    } catch (e) {
      s.passed = false
      // eslint-disable-next-line no-control-regex
      s.error = (e instanceof Error ? e.message : String(e)).replace(/\x1b\[[0-9;]*m/g, '').split('\n').slice(0, 8).join('\n')
      note(`#${no} KHÔNG ĐẠT: ${s.error}`)
    }
  })
  note(`#${no} ${s.passed ? 'Đạt' : 'Không đạt'}${s.notes.length ? ' — ' + s.notes.join('; ') : ''}`)
  save()
}

async function shot(s: Situation, page: Page, name: string) {
  const file = `${String(s.no).padStart(2, '0')}-${name}.png`
  await page.screenshot({ path: path.join(OUT, file) }).catch(() => undefined)
  s.screenshots.push(file)
}

async function staffSignsIn(page: Page, account: { username: string; password: string }) {
  await page.goto('/staff')
  await page.getByLabel('Username').fill(account.username)
  await page.getByLabel('Password').fill(account.password)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page.getByRole('navigation', { name: 'Staff portal' })).toBeVisible()
}

async function logIn(page: Page, slip: Slip) {
  await page.goto('/')
  await page.getByLabel('Username').fill(slip.username)
  await page.getByLabel('Password').fill(slip.password)
  await page.getByRole('button', { name: 'Login' }).click()
  await page.getByRole('button', { name: 'Next' }).click()
  await expect(page.getByLabel('PIN code')).toBeVisible()
}

/** PIN, "Unlock and Select", Next, then the readiness check (Confirm, forward arrow), as the exam day booklet says. */
async function typePin(page: Page, pin: string) {
  await page.getByLabel('PIN code').fill(pin)
  await page.getByRole('button', { name: 'Unlock and Select' }).click()
}

async function enterPin(page: Page, pin: string) {
  await typePin(page, pin)
  await page.getByRole('button', { name: 'Next' }).click()
  await page.getByRole('button', { name: /^(Start|Watch again)$/ }).click()
  await page.getByRole('button', { name: 'Confirm' }).click()
  await page.getByRole('button', { name: 'Continue' }).click()
}

/** Dashboard → instructions → "I confirm" → "Start (name)". */
async function openSection(page: Page, title: string) {
  await page.getByRole('button', { name: `Open ${title}` }).click()
  const start = page.getByRole('button', { name: `Start ${title}` })
  if (await start.isVisible().catch(() => false)) {
    await page.getByRole('button', { name: 'I confirm' }).click()
    await start.click()
  }
}

async function getJson<T>(page: Page, url: string): Promise<T> {
  const response = await page.request.get(url)
  expect(response.ok(), `${url} → ${response.status()}`).toBeTruthy()
  return (await response.json()) as T
}

const monitor = (page: Page, examId: number, sectionId: number) => getJson<{ candidates: MonitorRow[] }>(page, `/api/staff/exams/${examId}/sections/${sectionId}/monitor`).then((v) => v.candidates)

async function rowOf(page: Page, examId: number, sectionId: number, name: string) {
  const row = (await monitor(page, examId, sectionId)).find((r) => r.fullName === name)
  if (!row) {
    throw new Error(`${name} is not on the monitoring page`)
  }

  return row
}

/** Polls the monitoring data until the candidate's row satisfies the check; returns the row and how long it took. */
async function waitForRow(page: Page, examId: number, sectionId: number, name: string, check: (r: MonitorRow) => boolean, what: string, timeoutMs = 30_000) {
  const start = Date.now()
  for (;;) {
    const row = await rowOf(page, examId, sectionId, name)
    if (check(row)) {
      return { row, ms: Date.now() - start }
    }

    if (Date.now() - start > timeoutMs) {
      throw new Error(`${name}: ${what} not seen within ${timeoutMs / 1000} s (status ${row.status}, online ${row.isOnline}, warnings ${row.warnings})`)
    }

    await page.waitForTimeout(1_000)
  }
}

/** On the monitoring page: select these candidates, run one toolbar action, and clear the selection. */
async function act(portal: Page, names: string[], button: string, fill: (dialog: Locator) => Promise<void>) {
  for (const name of names) {
    await portal.getByRole('checkbox', { name: `Select ${name}`, exact: true }).check()
  }

  await portal.getByRole('toolbar', { name: 'Actions for the selected candidates' }).getByRole('button', { name: button, exact: true }).click()
  await fill(portal.getByRole('dialog'))
  await expect(portal.getByRole('dialog')).toBeHidden()
  await portal.getByRole('toolbar', { name: 'Actions for the selected candidates' }).getByRole('button', { name: 'Clear' }).click()
}

const examIdOf = (page: Page) => Number(/\/staff\/exams\/(\d+)/.exec(page.url())?.[1])

async function sectionIds(page: Page, examId: number) {
  const exam = await getJson<{ sections: { id: number; title: string }[] }>(page, `/api/staff/exams/${examId}`)
  return Object.fromEntries(exam.sections.map((s) => [s.title, s.id])) as Record<string, number>
}

/** Admin: Exams → New exam, the papers (a mix when one exists and is wanted), PIN, Active, candidates, staff, slips. */
async function prepareExam(page: Page, options: { name: string; type: string; sections: string[]; useMix: boolean; pin: string; people: Person[] }) {
  await page.goto('/staff/exams/new')
  const form = page.locator('form.st-form')
  await form.getByLabel('Name').fill(options.name)
  await form.locator('label.st-field').filter({ hasText: /^Exam/ }).locator('select').selectOption({ label: options.type })
  await form.getByLabel('Room').fill('Phòng máy 1')
  for (const section of options.sections) {
    const select = form.locator('label.st-field').filter({ hasText: new RegExp(`^${section} ·`) }).locator('select')
    const values = await select.locator('option').evaluateAll((all) => all.map((o) => (o as HTMLOptionElement).value).filter(Boolean))
    const value = (options.useMix && values.find((v) => v.startsWith('m:'))) || values.find((v) => v.startsWith('p:'))
    if (!value) {
      throw new Error(`No published ${section} paper for ${options.type}`)
    }

    await select.selectOption(value)
  }

  await form.locator('input[type=number]').fill('4')
  await form.getByLabel('Session PIN').fill(options.pin)
  await form.getByLabel('Invigilator password').fill('thithu-2026')
  await form.getByRole('button', { name: 'Create exam' }).click()
  await expect(page).toHaveURL(/\/staff\/exams\/\d+$/)
  const examId = examIdOf(page)

  await page.getByRole('button', { name: 'Add candidates' }).click()
  const rows = options.people.map((p) => [p.number, p.family, p.given, p.extra, p.seat].join('\t'))
  await page.getByRole('textbox', { name: 'Candidate rows' }).fill(rows.join('\n'))
  await page.getByRole('button', { name: `Add ${options.people.length}` }).click()
  await expect(page.getByRole('heading', { name: new RegExp(`Candidates \\(${options.people.length}\\)`) })).toBeVisible()

  // The boxes follow the server's answer, so click and wait rather than check().
  for (const account of [/giamthi/, /giamkhao/]) {
    await page.getByRole('checkbox', { name: account }).click()
    await expect(page.getByRole('checkbox', { name: account })).toBeChecked()
  }
  // Draft → Scheduled → Active, each with the portal's second click (the exam needs its candidates and invigilator first).
  const statusGroup = page.getByRole('group', { name: 'Change status' })
  for (const next of ['Scheduled', 'Active']) {
    await statusGroup.getByRole('button', { name: next, exact: true }).click()
    await statusGroup.getByRole('button', { name: `Click again: set ${next}`, exact: true }).click()
    await expect(statusGroup.getByRole('button', { name: next, exact: true })).toBeDisabled()
  }

  const [slipsPage] = await Promise.all([page.waitForEvent('popup'), page.getByRole('link', { name: 'Print login slips' }).click()])
  await expect(slipsPage.locator('article.slip')).toHaveCount(options.people.length)
  const slips = await slipsPage.locator('article.slip').evaluateAll((all) =>
    all.map((el) => {
      const who = el.querySelector('.slip-who')?.textContent ?? ''
      const values = [...el.querySelectorAll('dd')].map((d) => d.textContent ?? '')
      return { who, username: values[0], password: values[1] }
    }),
  )
  const byNumber = new Map<string, Slip>()
  for (const s of slips) {
    const [name, number] = s.who.split('·').map((x) => x.trim())
    byNumber.set(number, { number, name, username: s.username, password: s.password })
  }

  return { examId, slipsPage, byNumber }
}

test('mock exam: the 16 situations of KICH_BAN_THI_THU.md', async ({ browser }) => {
  fs.mkdirSync(OUT, { recursive: true })
  note(`Kết quả ghi vào ${OUT}`)

  // ── Preparation (the day before): the administrator sets up both exams in the staff portal ──
  const adminContext = await browser.newContext()
  const adminPage = await adminContext.newPage()
  await staffSignsIn(adminPage, admin)
  const a2 = await prepareExam(adminPage, {
    name: `Thi thử A2 ${day}`,
    type: 'A2 Key for Schools',
    sections: ['Reading & Writing', 'Listening'],
    useMix: false,
    pin: A2_PIN,
    people: A2_PEOPLE,
  })
  await a2.slipsPage.screenshot({ path: path.join(OUT, '00-phieu-dang-nhap-A2.png'), fullPage: true })
  await a2.slipsPage.close()
  const a2Sections = await sectionIds(adminPage, a2.examId)
  note(`Kỳ thi A2 #${a2.examId}, phần thi ${JSON.stringify(a2Sections)}`)

  const b1 = await prepareExam(adminPage, {
    name: `Thi thử B1 ${day}`,
    type: 'B1 Preliminary for Schools',
    sections: ['Reading', 'Writing', 'Listening'],
    useMix: true,
    pin: B1_PIN,
    people: B1_PEOPLE,
  })
  await b1.slipsPage.close()
  const b1Sections = await sectionIds(adminPage, b1.examId)
  const [roomPlan] = await Promise.all([adminPage.waitForEvent('popup'), adminPage.getByRole('link', { name: 'Room plan' }).click()])
  await expect(roomPlan.getByRole('listitem', { name: 'Computer 1' })).toBeVisible()
  await roomPlan.screenshot({ path: path.join(OUT, '00-so-do-phong-B1.png'), fullPage: true })
  await roomPlan.close()
  note(`Kỳ thi B1 #${b1.examId}, phần thi ${JSON.stringify(b1Sections)}`)

  const rw = a2Sections['Reading & Writing']
  const listening = a2Sections['Listening']

  // Each candidate at their own computer (browser context).
  const candidates: Candidate[] = []
  for (const person of A2_PEOPLE.slice(0, 6)) {
    const context = await browser.newContext()
    const slip = a2.byNumber.get(person.number)
    if (!slip) {
      throw new Error(`No login slip for ${person.number}`)
    }

    candidates.push({ person, name: fullName(person), slip, context, page: await context.newPage() })
  }

  const [an, hoa, binh, chi, dung, giang] = candidates
  const absent = fullName(A2_PEOPLE[6])

  const invigilatorContext = await browser.newContext()
  const portal = await invigilatorContext.newPage()
  await staffSignsIn(portal, invigilator)

  // ── At the start ──
  await situation(1, 'Thí sinh đăng nhập bằng phiếu và mã PIN', async (s) => {
    for (const c of candidates) {
      await logIn(c.page, c.slip)
    }

    for (const c of [hoa, binh, chi, dung, giang]) {
      await enterPin(c.page, A2_PIN)
      await expect(c.page.getByRole('button', { name: 'Open Reading & Writing' })).toBeEnabled()
      await expect(c.page.getByRole('button', { name: 'Listening is locked' })).toBeDisabled()
    }

    s.notes.push('6 thí sinh đăng nhập bằng tên và mật khẩu in trên phiếu; sau PIN vào màn hình chọn phần thi, Listening bị khoá (thí sinh 0101 nhập PIN ở tình huống 2)')
    await shot(s, hoa.page, 'chon-phan-thi')
  })

  await situation(2, 'Nhập sai mã PIN', async (s) => {
    for (const wrong of ['0000', '1111', '9999']) {
      await typePin(an.page, wrong)
      const alert = an.page.getByRole('alert')
      await expect(alert).toBeVisible()
      if (s.notes.length === 0) {
        s.notes.push(`Sai PIN: "${(await alert.textContent())?.trim()}"`)
      }
    }

    await shot(s, an.page, 'sai-pin')
    await enterPin(an.page, A2_PIN)
    await expect(an.page.getByRole('button', { name: 'Open Reading & Writing' })).toBeEnabled()
    await expect(an.page.getByRole('button', { name: 'Listening is locked' })).toBeDisabled()
    s.notes.push('3 lần sai đều bị từ chối, lần đúng vào được')
  })

  await portal.goto(`/staff/exams/${a2.examId}/sections/${rw}`)
  await expect(portal.getByText('live')).toBeVisible()

  await situation(3, 'Bắt đầu trước khi giám thị Announce start (cảnh báo StartedEarly)', async (s) => {
    await openSection(binh.page, 'Reading & Writing')
    await expect(binh.page.getByRole('radiogroup', { name: 'Answers for question 1' })).toBeVisible()
    await waitForRow(portal, a2.examId, rw, binh.name, (r) => r.status === 'InProgress', 'InProgress')

    await portal.getByRole('button', { name: 'Announce start' }).click()
    await portal.getByRole('button', { name: 'Click again: the room may begin' }).click()
    await expect(portal.getByText(/Start announced/)).toBeVisible()
    await expect(portal.getByRole('button', { name: `1 warning for ${binh.name}` })).toBeVisible()

    for (const c of [an, hoa, chi, dung, giang]) {
      await openSection(c.page, 'Reading & Writing')
      await expect(c.page.getByRole('radiogroup', { name: 'Answers for question 1' })).toBeVisible()
    }

    const rows = await monitor(portal, a2.examId, rw)
    const flagged = rows.filter((r) => r.warnings > 0).map((r) => r.fullName)
    expect(flagged).toEqual([binh.name])
    s.notes.push(`Chỉ ${binh.name} (bắt đầu trước) có cảnh báo; 5 thí sinh bắt đầu sau không có`)
    await portal.getByRole('button', { name: `1 warning for ${binh.name}` }).click()
    await expect(portal.getByRole('dialog').getByText('Started early')).toBeVisible()
    await shot(s, portal, 'canh-bao-started-early')
    await portal.getByRole('dialog').getByRole('button', { name: 'Close' }).click()
  })

  await situation(4, 'Làm bài, ghi chú/highlight, đổi cỡ chữ và độ tương phản', async (s) => {
    const page = an.page
    const saved = page.waitForResponse((r) => r.url().includes('/responses') && r.request().method() === 'PUT' && r.status() === 204)
    await page.getByRole('radiogroup', { name: 'Answers for question 1' }).getByRole('radio').nth(1).check()
    await saved
    await expect(page.getByRole('img', { name: 'Connected' })).toBeVisible()
    s.notes.push('Câu 1 lưu ngay (PUT 204), biểu tượng kết nối "Connected"')

    await page.getByRole('button', { name: /^Part 3/ }).click()
    const paragraph = page.locator('.part-page p').filter({ hasText: /\w{4,}/ }).first()
    await paragraph.click({ clickCount: 3 })
    await page.getByRole('toolbar', { name: 'Selected text' }).getByRole('button', { name: 'Highlight' }).click()
    const highlights = await page.evaluate(() => [...(CSS as unknown as { highlights: Map<string, { size: number }> }).highlights.values()].reduce((n, h) => n + h.size, 0))
    expect(highlights).toBeGreaterThan(0)
    s.notes.push(`Highlight một đoạn ở Part 3 (${highlights} vùng tô)`)

    await page.getByRole('button', { name: 'Options', exact: true }).click()
    const options = page.getByRole('dialog', { name: 'Options' })
    const preferences = page.waitForResponse((r) => r.url().includes('/preferences') && r.ok())
    await options.getByRole('radio', { name: 'White on black' }).check()
    await preferences
    await options.getByRole('radio', { name: 'Large', exact: true }).check()
    await expect(page.locator('.exam[data-contrast="WhiteOnBlack"][data-text-size="Large"]')).toHaveCount(1)
    await options.getByRole('button', { name: 'Close options' }).click()
    await shot(s, page, 'trang-tren-den-chu-lon')
    s.notes.push('Đổi độ tương phản "White on black" và cỡ chữ "Large" có hiệu lực ngay, được lưu lên máy chủ')

    await page.getByRole('button', { name: 'Options', exact: true }).click()
    await options.getByRole('radio', { name: 'Black on white' }).check()
    await options.getByRole('radio', { name: 'Regular', exact: true }).check()
    await options.getByRole('button', { name: 'Close options' }).click()
    await expect(page.locator('.exam[data-contrast="BlackOnWhite"]')).toHaveCount(1)
  })

  await situation(5, 'Tải lại trang giữa giờ', async (s) => {
    const page = an.page
    const before = await rowOf(portal, a2.examId, rw, an.name)
    const t0 = Date.now()
    await page.reload()
    await expect(page.getByRole('radiogroup', { name: 'Answers for question 14' })).toBeVisible()
    s.notes.push('Sau F5 vẫn ở Part 3 của Reading & Writing')
    const highlights = await page.evaluate(() => [...(CSS as unknown as { highlights: Map<string, { size: number }> }).highlights.values()].reduce((n, h) => n + h.size, 0))
    expect(highlights).toBeGreaterThan(0)
    await expect(page.getByRole('timer')).toHaveText(/minutes? left/)
    const after = await rowOf(portal, a2.examId, rw, an.name)
    const drift = (before.remainingSeconds ?? 0) - (after.remainingSeconds ?? 0) - Math.round((Date.now() - t0) / 1000)
    expect(Math.abs(drift)).toBeLessThanOrEqual(3)
    await page.getByRole('button', { name: /^Part 1/ }).click()
    await expect(page.getByRole('radiogroup', { name: 'Answers for question 1' }).getByRole('radio').nth(1)).toBeChecked()
    s.notes.push(`Còn đáp án câu 1 và highlight; đồng hồ lệch ${drift} s so với thời gian đã trôi`)
    await shot(s, page, 'sau-khi-tai-lai')
  })

  // Writing for the examiner (situation 15): two candidates write both Writing questions.
  for (const [c, words] of [
    [an, 'Dear Sam, I am at the park with my brother. We are playing football and it is sunny. See you soon, An'],
    [hoa, 'Hi Lan, I went to the museum yesterday. I saw old boats and a big map. It was great! Hoa'],
  ] as const) {
    await c.page.getByRole('button', { name: /^Part 6/ }).click()
    await c.page.getByRole('textbox', { name: 'Question 31' }).fill(words)
    await c.page.getByRole('button', { name: /^Part 7/ }).click()
    const saved = c.page.waitForResponse((r) => r.url().includes('/responses') && r.request().method() === 'PUT' && r.status() === 204)
    await c.page.getByRole('textbox', { name: 'Question 32' }).fill(`${words} The story ends when everyone goes home happy.`)
    await saved
  }

  // 0101 also answers Part 1 (questions 1–6) from the answer key, so situation 15 can check the automatic mark.
  const sample = JSON.parse(fs.readFileSync(path.resolve(process.cwd(), '../../setup/exam-package/samples/a2-key-fs-reading-writing/paper.json'), 'utf8')) as {
    parts: { groups: { questions: { number: number; options: { key: string }[]; answers: string[] }[] }[] }[]
  }
  await an.page.getByRole('button', { name: /^Part 1/ }).click()
  for (const q of sample.parts[0].groups[0].questions) {
    await an.page.getByRole('button', { name: `Question ${q.number}`, exact: true }).click()
    const saved = an.page.waitForResponse((r) => r.url().includes('/responses') && r.request().method() === 'PUT' && r.status() === 204)
    await an.page.getByRole('radiogroup', { name: `Answers for question ${q.number}` }).getByRole('radio').nth(q.options.findIndex((o) => o.key === q.answers[0])).check()
    await saved
  }

  note('0101 và 0102 đã viết câu 31, 32; 0101 trả lời đúng Part 1 (câu 1–6)')

  await situation(6, 'Tạm dừng một thí sinh, cả phòng; cho làm tiếp', async (s) => {
    await act(portal, [binh.name], 'Pause', async (dialog) => {
      const t0 = Date.now()
      await dialog.getByRole('button', { name: 'Submit event' }).click()
      await expect(binh.page.getByRole('alertdialog', { name: 'Test paused' })).toBeVisible({ timeout: 5_000 })
      s.notes.push(`Dừng 1 thí sinh: màn hình dừng sau ${Date.now() - t0} ms`)
    })
    const paused = await rowOf(portal, a2.examId, rw, binh.name)
    await portal.waitForTimeout(10_000)
    const stillPaused = await rowOf(portal, a2.examId, rw, binh.name)
    expect(stillPaused.status).toBe('Paused')
    expect(stillPaused.remainingSeconds).toBe(paused.remainingSeconds)
    await shot(s, binh.page, 'thi-sinh-bi-dung')
    await act(portal, [binh.name], 'Resume', (dialog) => dialog.getByRole('button', { name: 'Resume' }).click())
    await expect(binh.page.getByRole('alertdialog', { name: 'Test paused' })).toBeHidden({ timeout: 5_000 })
    const resumed = await rowOf(portal, a2.examId, rw, binh.name)
    expect((paused.remainingSeconds ?? 0) - (resumed.remainingSeconds ?? 0)).toBeLessThanOrEqual(3)
    s.notes.push(`Trong 10 s dừng đồng hồ đứng yên (${paused.remainingSeconds} s), sau khi cho làm tiếp còn ${resumed.remainingSeconds} s`)

    await portal.getByRole('checkbox', { name: 'Select all candidates' }).check()
    await portal.getByRole('toolbar', { name: 'Actions for the selected candidates' }).getByRole('button', { name: 'Pause', exact: true }).click()
    const t0 = Date.now()
    await portal.getByRole('dialog').getByRole('button', { name: 'Submit event' }).click()
    const latencies = await Promise.all(
      candidates.map(async (c) => {
        await expect(c.page.getByRole('alertdialog', { name: 'Test paused' })).toBeVisible({ timeout: 5_000 })
        return Date.now() - t0
      }),
    )
    await expect(portal.getByRole('status').filter({ hasText: 'Paused: done for 6 candidates' })).toBeVisible()
    await expect(portal.getByRole('status').filter({ hasText: absent })).toBeVisible()
    s.notes.push(`Dừng cả phòng: 6/6 màn hình dừng sau tối đa ${Math.max(...latencies)} ms; ${absent} (chưa vào thi) được báo bỏ qua`)
    await shot(s, portal, 'ca-phong-tam-dung')
    await portal.getByRole('toolbar', { name: 'Actions for the selected candidates' }).getByRole('button', { name: 'Resume', exact: true }).click()
    await portal.getByRole('dialog').getByRole('button', { name: 'Resume' }).click()
    for (const c of candidates) {
      await expect(c.page.getByRole('alertdialog', { name: 'Test paused' })).toBeHidden({ timeout: 5_000 })
    }

    await portal.getByRole('toolbar', { name: 'Actions for the selected candidates' }).getByRole('button', { name: 'Clear' }).click()
  })

  await situation(7, 'Thông báo cho cả phòng và cho một thí sinh', async (s) => {
    const toRoom = 'Các em còn khoảng 40 phút, nhớ kiểm tra lại đáp án.'
    await portal.getByRole('button', { name: 'Message the room' }).click()
    await portal.getByRole('dialog').getByLabel('Message').fill(toRoom)
    const t0 = Date.now()
    await portal.getByRole('dialog').getByRole('button', { name: 'Send' }).click()
    const latencies = await Promise.all(
      candidates.map(async (c) => {
        const popup = c.page.getByRole('alertdialog', { name: 'Message from the invigilator' })
        await expect(popup).toContainText(toRoom, { timeout: 12_000 })
        return Date.now() - t0
      }),
    )
    s.notes.push(`Thông báo cả phòng đến 6/6 màn hình sau tối đa ${Math.max(...latencies)} ms`)
    await shot(s, an.page, 'thong-bao-ca-phong')
    for (const c of candidates) {
      await c.page.getByRole('alertdialog', { name: 'Message from the invigilator' }).getByRole('button', { name: 'OK' }).click()
    }

    const toOne = 'Em Thị Hoa giơ tay để giám thị hỗ trợ nhé.'
    await act(portal, [hoa.name], 'Message', async (dialog) => {
      await dialog.getByLabel('Message').fill(toOne)
      await dialog.getByRole('button', { name: 'Send' }).click()
    })
    await expect(hoa.page.getByRole('alertdialog', { name: 'Message from the invigilator' })).toContainText(toOne, { timeout: 12_000 })
    await an.page.waitForTimeout(6_000)
    await expect(an.page.getByText(toOne)).toHaveCount(0)
    await an.page.getByRole('button', { name: /^Messages/ }).click()
    await expect(an.page.getByRole('list', { name: 'Messages from the invigilator' })).toContainText(toRoom)
    await expect(an.page.getByRole('list', { name: 'Messages from the invigilator' })).not.toContainText(toOne)
    await an.page.getByRole('button', { name: /^Messages/ }).click()
    await hoa.page.getByRole('alertdialog', { name: 'Message from the invigilator' }).getByRole('button', { name: 'OK' }).click()
    s.notes.push(`Thông báo riêng chỉ đến ${hoa.name}; thí sinh khác chỉ thấy thông báo chung trong chuông`)
  })

  await situation(8, 'Mất mạng 1 phút rồi vào lại; cộng giờ bù', async (s) => {
    const before = await rowOf(portal, a2.examId, rw, chi.name)
    const t0 = Date.now()
    await chi.context.setOffline(true)
    await chi.page.getByRole('radiogroup', { name: 'Answers for question 1' }).getByRole('radio').nth(2).check()
    await expect(chi.page.getByRole('img', { name: /Not connected/ })).toBeVisible({ timeout: 20_000 })
    await waitForRow(portal, a2.examId, rw, chi.name, (r) => !r.isOnline, 'Offline', 45_000)
    const offlineAfter = Math.round((Date.now() - t0) / 1000)
    await waitForRow(portal, a2.examId, rw, chi.name, (r) => r.warnings > 0, 'ConnectionLost', 45_000)
    s.notes.push(`Trang giám thị báo Offline sau ${offlineAfter} s, có cảnh báo ConnectionLost`)
    await expect(portal.getByRole('row', { name: new RegExp(chi.name) }).getByText('Offline')).toBeVisible()
    await shot(s, portal, 'offline')
    await shot(s, chi.page, 'thi-sinh-mat-mang')

    await chi.page.waitForTimeout(Math.max(0, 60_000 - (Date.now() - t0)))
    await chi.context.setOffline(false)
    const back = await waitForRow(portal, a2.examId, rw, chi.name, (r) => r.isOnline && r.answered >= 1, 'Online again with the answer saved', 45_000)
    await expect(chi.page.getByRole('img', { name: 'Connected' })).toBeVisible({ timeout: 20_000 })
    const elapsed = Math.round((Date.now() - t0) / 1000)
    const ran = (before.remainingSeconds ?? 0) - (back.row.remainingSeconds ?? 0)
    expect(Math.abs(ran - elapsed)).toBeLessThanOrEqual(5)
    s.notes.push(`Mất mạng ${elapsed} s: đồng hồ vẫn chạy (${ran} s), đáp án chọn lúc mất mạng được lưu khi có mạng lại`)

    await act(portal, [chi.name], 'Incident extra time', async (dialog) => {
      await dialog.getByLabel('Minutes to add').fill('1')
      await dialog.getByRole('button', { name: 'Update' }).click()
    })
    await waitForRow(portal, a2.examId, rw, chi.name, (r) => r.incidentExtraSeconds === 60, 'incident time 1 min', 10_000)
    s.notes.push('Giám thị cộng 1 phút giờ bù')
  })

  await situation(9, 'Chuyển sang máy khác', async (s) => {
    const saved = dung.page.waitForResponse((r) => r.url().includes('/responses') && r.request().method() === 'PUT' && r.status() === 204)
    await dung.page.getByRole('radiogroup', { name: 'Answers for question 1' }).getByRole('radio').first().check()
    await saved

    const oldPage = dung.page
    const oldContext = dung.context
    const context = await browser.newContext()
    const page = await context.newPage()
    await logIn(page, dung.slip)
    await enterPin(page, A2_PIN)
    await expect(oldPage.getByLabel('Username')).toBeVisible({ timeout: 20_000 })
    s.notes.push('Máy cũ tự về màn hình đăng nhập')
    await openSection(page, 'Reading & Writing')
    await expect(page.getByRole('radiogroup', { name: 'Answers for question 1' }).getByRole('radio').first()).toBeChecked()
    s.notes.push('Máy mới tiếp tục cùng lượt làm bài, còn đáp án câu 1')
    await shot(s, page, 'may-moi')
    // What was read lives in the browser tab, so the new computer shows the room message once more.
    const popup = page.getByRole('alertdialog', { name: 'Message from the invigilator' })
    await expect(popup).toBeVisible({ timeout: 12_000 })
    await popup.getByRole('button', { name: 'OK' }).click()
    s.notes.push('Máy mới hiện lại thông báo chung đã đọc ở máy cũ một lần (ghi nhận, không phải lỗi)')
    await oldContext.close()
    dung.context = context
    dung.page = page
  })

  await situation(10, 'Cộng giờ bù sự cố; Edit end time (End in)', async (s) => {
    const before = await rowOf(portal, a2.examId, rw, an.name)
    await act(portal, [an.name], 'Incident extra time', async (dialog) => {
      await dialog.getByLabel('Minutes to add').fill('5')
      await dialog.getByRole('button', { name: 'Update' }).click()
    })
    const { row: after } = await waitForRow(portal, a2.examId, rw, an.name, (r) => r.incidentExtraSeconds === 300, 'incident time 5 min', 10_000)
    const gained = (after.remainingSeconds ?? 0) - (before.remainingSeconds ?? 0)
    expect(gained).toBeGreaterThanOrEqual(290)
    expect(gained).toBeLessThanOrEqual(300)
    const minutes = Math.ceil((after.remainingSeconds ?? 0) / 60)
    await expect(an.page.getByRole('timer')).toHaveText(new RegExp(`^(${minutes}|${minutes - 1}) minutes left`), { timeout: 12_000 })
    s.notes.push(`${an.name} +5 phút: thời gian còn lại tăng ${gained} s, màn hình thí sinh cập nhật`)

    await act(portal, [dung.name], 'Edit end time', async (dialog) => {
      await dialog.getByLabel('Minutes from now').fill('2')
      await dialog.getByRole('button', { name: 'Apply' }).click()
    })
    const { row: ending } = await waitForRow(portal, a2.examId, rw, dung.name, (r) => r.endTimeEdited, 'end edited', 10_000)
    expect(ending.remainingSeconds).toBeGreaterThanOrEqual(110)
    expect(ending.remainingSeconds).toBeLessThanOrEqual(121)
    await expect(dung.page.getByRole('timer')).toHaveText(/^[0-2]:\d\d left/, { timeout: 12_000 })
    await expect(portal.getByRole('row', { name: new RegExp(dung.name) }).getByText('end edited')).toBeVisible()
    await shot(s, portal, 'gio-bu-va-end-in')
    s.notes.push(`${dung.name} End in 2 phút: còn ${ending.remainingSeconds} s, màn hình thí sinh đếm phút và giây (dưới 5 phút)`)

    const expiry = await waitForRow(portal, a2.examId, rw, dung.name, (r) => r.status === 'Submitted', 'submitted at the edited end', 150_000)
    expect(expiry.row.submitReason).toBe('TimeExpired')
    await expect(dung.page.getByText('The test has now finished. Your answers have been saved.')).toBeVisible()
    s.notes.push(`Hết 2 phút ${dung.name} tự nộp (TimeExpired), màn hình báo kết thúc`)
  })

  await situation(13, 'End now cho một thí sinh', async (s) => {
    await act(portal, [giang.name], 'Edit end time', async (dialog) => {
      await dialog.getByRole('radio', { name: 'End now' }).check()
      await expect(dialog.getByText('End now cannot be undone at section level.')).toBeVisible()
      await dialog.getByRole('button', { name: 'End now' }).click()
      const t0 = Date.now()
      await dialog.getByRole('button', { name: 'Click again to end now' }).click()
      await expect(giang.page.getByText('The test has now finished. Your answers have been saved.')).toBeVisible({ timeout: 10_000 })
      s.notes.push(`Hai lần bấm; màn hình thí sinh kết thúc sau ${Date.now() - t0} ms`)
    })
    const row = await rowOf(portal, a2.examId, rw, giang.name)
    expect(row.status).toBe('Submitted')
    expect(row.submitReason).toBe('InvigilatorEndNow')
    await expect(portal.getByRole('row', { name: new RegExp(giang.name) }).getByText('Ended by invigilator')).toBeVisible()
    await portal.getByRole('checkbox', { name: `Select ${giang.name}`, exact: true }).check()
    const toolbar = portal.getByRole('toolbar', { name: 'Actions for the selected candidates' })
    for (const button of ['Pause', 'Resume', 'Incident extra time', 'Edit end time']) {
      await expect(toolbar.getByRole('button', { name: button, exact: true })).toBeDisabled()
    }

    await shot(s, portal, 'end-now')
    await toolbar.getByRole('button', { name: 'Clear' }).click()
    s.notes.push('Trạng thái Submitted, "Ended by invigilator"; mọi thao tác đổi giờ bị khoá nên không hoàn tác được')
  })

  // The rest of the room finishes Reading & Writing a minute from now, then starts Listening.
  await act(portal, [an.name, hoa.name, binh.name, chi.name], 'Edit end time', async (dialog) => {
    await dialog.getByLabel('Minutes from now').fill('1')
    await dialog.getByRole('button', { name: 'Apply' }).click()
  })
  for (const c of [an, hoa, binh, chi]) {
    await waitForRow(portal, a2.examId, rw, c.name, (r) => r.status === 'Submitted', 'R&W submitted', 120_000)
  }

  note('Reading & Writing kết thúc cho cả phòng')
  for (const c of candidates) {
    await expect(c.page.getByText('The test has now finished. Your answers have been saved.')).toBeVisible({ timeout: 15_000 })
    await openSection(c.page, 'Listening')
    await c.page.getByRole('dialog', { name: 'Listening' }).getByRole('button', { name: 'Play' }).click()
  }

  const listeningStarted = Date.now()
  note('Cả phòng bắt đầu Listening')
  await portal.goto(`/staff/exams/${a2.examId}/sections/${listening}`)
  await expect(portal.getByText('live')).toBeVisible()

  await situation(11, 'Listening: tua lại 1 phút và cộng 1 phút', async (s) => {
    // The room message read during Reading & Writing must not pop up again now that Listening is open.
    await an.page.waitForTimeout(6_000)
    for (const c of candidates) {
      await expect(c.page.getByRole('alertdialog', { name: 'Message from the invigilator' })).toHaveCount(0)
    }

    s.notes.push('Sang Listening, thông báo đã đọc ở Reading & Writing không bật lại')
    const { row: before } = await waitForRow(portal, a2.examId, listening, an.name, (r) => (r.audioPositionSeconds ?? 0) >= 75, 'audio past 75 s', 120_000)
    await portal.getByRole('checkbox', { name: `Select ${an.name}`, exact: true }).check()
    await portal.getByRole('toolbar', { name: 'Actions for the selected candidates' }).getByRole('button', { name: 'Rewind audio', exact: true }).click()
    await portal.getByRole('dialog').getByLabel('Rewind by (minutes)').fill('1')
    await portal.getByRole('dialog').getByRole('button', { name: 'Rewind now' }).click()
    const extra = portal.getByRole('dialog', { name: 'Incident extra time' })
    await expect(extra.getByLabel('Minutes to add')).toHaveValue('1')
    await shot(s, portal, 'tua-xong-mo-cong-gio')
    await expect(an.page.getByText('The invigilator has moved the recording back.')).toBeVisible({ timeout: 12_000 })
    await shot(s, an.page, 'thi-sinh-nghe-lai')
    s.notes.push('Màn hình thí sinh báo "The invigilator has moved the recording back." và phát lại từ chỗ mới')
    await extra.getByRole('button', { name: 'Update' }).click()
    await portal.getByRole('toolbar', { name: 'Actions for the selected candidates' }).getByRole('button', { name: 'Clear' }).click()
    const { row: after } = await waitForRow(portal, a2.examId, listening, an.name, (r) => r.incidentExtraSeconds === 60, 'incident time 1 min', 10_000)
    const back = (before.audioPositionSeconds ?? 0) - (after.audioPositionSeconds ?? 0)
    expect(back).toBeGreaterThanOrEqual(55)
    expect(back).toBeLessThanOrEqual(61)
    s.notes.push(`Vị trí audio ${before.audioPositionSeconds} s → ${after.audioPositionSeconds} s (lùi ${back} s); hộp cộng giờ mở sẵn 1 phút, cộng xong 60 s`)

  })

  // A computer that goes off for good: its Listening must still end on time (situation 14).
  await chi.context.close()
  note(`${chi.name}: máy tắt hẳn giữa Listening`)

  await situation(12, 'B1 Writing Part 2: chọn một trong hai câu', async (s) => {
    const person = B1_PEOPLE[0]
    const slip = b1.byNumber.get(person.number)
    if (!slip) {
      throw new Error('No B1 slip')
    }

    const context = await browser.newContext()
    const page = await context.newPage()
    await logIn(page, slip)
    await enterPin(page, B1_PIN)
    await expect(page.getByRole('button', { name: 'Writing is locked' })).toBeDisabled()
    await openSection(page, 'Reading')
    await expect(page.getByRole('radiogroup', { name: 'Answers for question 1' })).toBeVisible()

    const b1Portal = await invigilatorContext.newPage()
    await b1Portal.goto(`/staff/exams/${b1.examId}/sections/${b1Sections['Reading']}`)
    await expect(b1Portal.getByRole('row', { name: new RegExp(fullName(person)) }).getByText('Online')).toBeVisible({ timeout: 20_000 })
    await act(b1Portal, [fullName(person)], 'Edit end time', async (dialog) => {
      await dialog.getByRole('radio', { name: 'End now' }).check()
      await dialog.getByRole('button', { name: 'End now' }).click()
      await dialog.getByRole('button', { name: 'Click again to end now' }).click()
    })
    await expect(page.getByText('The test has now finished. Your answers have been saved.')).toBeVisible({ timeout: 10_000 })

    await openSection(page, 'Writing')
    await page.getByRole('button', { name: /^Part 2/ }).click()
    await expect(page.getByText('0 of 1 question selected')).toBeVisible()
    const chosen = page.waitForResponse((r) => r.url().includes('/selections') && r.status() === 204)
    await page.getByRole('combobox', { name: 'Answering question 2?' }).selectOption('Yes')
    await chosen
    await expect(page.getByText('1 of 1 question selected')).toBeVisible()
    await page.getByRole('textbox', { name: 'Question 2' }).fill('Last summer I visited my aunt in Da Lat. The weather was cool and we walked by the lake every evening.')
    await page.getByRole('button', { name: 'Question 3', exact: true }).click()
    const choice3 = page.getByRole('combobox', { name: 'Answering question 3?' })
    await expect(choice3.getByRole('option', { name: 'Yes' })).toHaveJSProperty('disabled', true)
    await shot(s, page, 'b1-chon-cau')
    const sectionId = b1Sections['Writing']
    const forced = await page.request.put(`/api/candidate/sections/${sectionId}/selections`, { data: { questionNumber: 3, selection: 'Yes' } })
    expect(forced.status()).toBe(409)
    s.notes.push('Chọn Yes câu 2: "1 of 1 question selected"; câu 3 không còn chọn Yes được; gọi thẳng API chọn thêm câu 3 bị từ chối 409')
    await b1Portal.close()
    await context.close()
  })

  await situation(16, 'Yêu cầu xoá thí sinh và admin khác duyệt', async (s) => {
    await adminPage.goto(`/staff/exams/${a2.examId}`)
    const row = adminPage.getByRole('row', { name: new RegExp(A2_PEOPLE[6].number) })
    await row.getByRole('button', { name: 'Request removal' }).click()
    await row.getByRole('button', { name: 'Ask an administrator to remove' }).click()
    await expect(adminPage.getByText('Sent for approval:')).toBeVisible()
    await expect(row).toBeVisible()
    s.notes.push(`Admin 1 yêu cầu xoá ${absent}: thí sinh chưa bị xoá, yêu cầu chờ duyệt`)

    await adminPage.getByRole('navigation', { name: 'Staff portal' }).getByRole('link', { name: 'Approvals' }).click()
    const own = adminPage.getByRole('row', { name: new RegExp(absent) })
    await expect(own.getByText('Another administrator must decide.')).toBeVisible()
    await expect(own.getByRole('button', { name: 'Approve' })).toHaveCount(0)
    s.notes.push('Người yêu cầu không có nút Approve cho yêu cầu của mình')

    const secondContext = await browser.newContext()
    const approver = await secondContext.newPage()
    await staffSignsIn(approver, secondAdmin)
    await approver.getByRole('navigation', { name: 'Staff portal' }).getByRole('link', { name: 'Approvals' }).click()
    const request = approver.getByRole('row', { name: new RegExp(absent) })
    await request.getByRole('button', { name: 'Approve' }).click()
    await request.getByRole('button', { name: 'Click again to approve' }).click()
    await approver.getByRole('button', { name: /^All/ }).click()
    await expect(approver.getByRole('row', { name: new RegExp(absent) }).getByText('Approved')).toBeVisible()
    await shot(s, approver, 'nhat-ky-duyet')
    await adminPage.goto(`/staff/exams/${a2.examId}`)
    await expect(adminPage.getByRole('heading', { name: /Candidates \(6\)/ })).toBeVisible()
    await expect(adminPage.getByRole('row', { name: new RegExp(A2_PEOPLE[6].number) })).toHaveCount(0)
    s.notes.push('admin2 duyệt: thí sinh bị xoá, nhật ký ghi Approved và người duyệt')
    await secondContext.close()
  })

  await situation(14, 'Hết giờ tự nộp (cả thí sinh thêm 25 % giờ, máy offline)', async (s) => {
    const names = candidates.map((c) => c.name)
    const firstSeen = new Map<string, { at: number; endsAt: number; reason: string | null }>()
    const lastEnds = new Map<string, number>()
    let hoaStillWorkingWhenOthersDone = false
    const deadline = listeningStarted + (LISTENING_MINUTES * 1.25 + 4) * 60_000
    while (firstSeen.size < names.length && Date.now() < deadline) {
      const rows = await monitor(portal, a2.examId, listening)
      for (const r of rows.filter((x) => names.includes(x.fullName))) {
        if (r.endsAt && r.status !== 'Submitted') {
          lastEnds.set(r.fullName, Date.parse(r.endsAt))
        }

        if (r.status === 'Submitted' && !firstSeen.has(r.fullName)) {
          firstSeen.set(r.fullName, { at: Date.now(), endsAt: lastEnds.get(r.fullName) ?? 0, reason: r.submitReason })
          note(`${r.fullName} nộp bài (${r.submitReason})`)
        }
      }

      if (firstSeen.size === names.length - 1 && !firstSeen.has(hoa.name)) {
        hoaStillWorkingWhenOthersDone = true
      }

      await portal.waitForTimeout(5_000)
    }

    for (const name of names) {
      const seen = firstSeen.get(name)
      expect(seen, `${name} submitted`).toBeTruthy()
      expect(seen!.reason, name).toBe('TimeExpired')
      const late = Math.round((seen!.at - seen!.endsAt) / 1000)
      expect(late, `${name} submitted ${late} s after the end`).toBeLessThanOrEqual(15)
      expect(late, `${name} submitted ${late} s after the end`).toBeGreaterThanOrEqual(-6)
    }

    const minutes = (name: string) => ((firstSeen.get(name)!.endsAt - listeningStarted) / 60_000).toFixed(1)
    expect(hoaStillWorkingWhenOthersDone).toBe(true)
    const ended = await monitor(portal, a2.examId, listening)
    expect(ended.filter((r) => names.includes(r.fullName)).map((r) => r.audioPositionSeconds)).toEqual(names.map(() => null))
    s.notes.push(`Cả 6 tự nộp (TimeExpired) trong vòng 15 s sau giờ kết thúc; thường ${minutes(binh.name)} phút, ${an.name} (+1 phút bù) ${minutes(an.name)} phút, ${hoa.name} (25 %) ${minutes(hoa.name)} phút, ${chi.name} (máy tắt) ${minutes(chi.name)} phút`)
    await expect(an.page.getByText('The test has now finished. Your answers have been saved.')).toBeVisible({ timeout: 15_000 })
    await shot(s, portal, 'het-gio')
  })

  await situation(15, 'Giám khảo chấm Writing ẩn danh, admin xem kết quả, xuất CSV', async (s) => {
    const examinerContext = await browser.newContext()
    const marking = await examinerContext.newPage()
    await staffSignsIn(marking, examiner)
    // VE-73: scores with names are for administrators; the examiner marks the script dealt to them by its code.
    const results = () => getJson<CandidateResult[]>(adminPage, `/api/staff/exams/${a2.examId}/results`)
    const rwOf = (all: CandidateResult[], name: string) => all.find((r) => r.fullName === name)!.sections.find((x) => x.sectionId === rw)!
    const before = rwOf(await results(), an.name)
    expect(before.gradingStatus).toBe('NeedsManualMarking')
    expect(before.rawScore).toBe(6)
    expect((await marking.request.get(`/api/staff/exams/${a2.examId}/results`)).status()).toBe(403)

    await marking.goto('/staff/marking')
    const code = (await results()).find((r) => r.fullName === an.name)!.markingCode!
    expect(code).toMatch(/^[A-HJ-NP-Z2-9]{6}$/)
    await marking.getByRole('row', { name: new RegExp(code) }).getByRole('link', { name: 'Mark Writing' }).click()
    await expect(marking.getByText(an.name)).toHaveCount(0)
    const marks: Record<number, number> = { 31: 3, 32: 4 }
    for (const number of [31, 32]) {
      const writing = marking.locator('section', { has: marking.getByRole('heading', { name: new RegExp(`Question ${number} ·`) }) })
      await writing.getByLabel(/Mark \(0–/).fill(String(marks[number]))
      await writing.getByRole('button', { name: 'Save mark' }).click()
      await expect(writing.getByText(/Saved/)).toBeVisible()
    }

    await shot(s, marking, 'cham-writing')
    const after = rwOf(await results(), an.name)
    expect(after.gradingStatus).toBe('Final')
    expect(after.rawScore).toBe((before.rawScore ?? 0) + 7)
    s.notes.push(`${an.name} (bài mã ${code}, giám khảo không thấy tên): chấm tự động ${before.rawScore} (đúng 6 câu Part 1 đã trả lời theo đáp án) + Writing 7 = ${after.rawScore}/${after.maxScore}, trạng thái Final`)

    await adminPage.goto(`/staff/exams/${a2.examId}/results`)
    await expect(adminPage.getByRole('row', { name: new RegExp(an.name) }).getByText('Final')).toBeVisible()
    await shot(s, adminPage, 'bang-ket-qua')
    const [download] = await Promise.all([adminPage.waitForEvent('download'), adminPage.getByRole('button', { name: 'Download for Excel (.csv)' }).click()])
    const file = path.join(OUT, 'ket-qua-thi-thu-A2.csv')
    await download.saveAs(file)
    const bytes = fs.readFileSync(file)
    expect([...bytes.subarray(0, 3)]).toEqual([0xef, 0xbb, 0xbf])
    const text = bytes.toString('utf8')
    for (const c of candidates) {
      expect(text).toContain(c.name)
    }

    s.notes.push('CSV có BOM UTF-8 (Excel đọc đúng tiếng Việt), đủ 6 thí sinh, điểm Reading & Writing và Listening')
    await examinerContext.close()
  })

  for (const c of candidates) {
    await c.context.close().catch(() => undefined)
  }

  await invigilatorContext.close()
  await adminContext.close()
  save()
  const failed = situations.filter((s) => !s.passed)
  note(`Xong: ${situations.length - failed.length}/${situations.length} tình huống đạt`)
  expect(failed.map((s) => `${s.no}. ${s.title}: ${s.error}`)).toEqual([])
})
