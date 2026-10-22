import { KEEP_ALIVE_MS, SERVER_TIMEOUT_MS } from './live'

// The API (Program.cs, AddSignalR): KeepAliveInterval 5 s, ClientTimeoutInterval 12 s.
const SERVER_KEEP_ALIVE_MS = 5_000
const SERVER_CLIENT_TIMEOUT_MS = 12_000

describe('live connection timing (VE-41)', () => {
  it('pings at least twice within the time the server waits, so a quiet page is never cut off', () => {
    expect(KEEP_ALIVE_MS * 2).toBeLessThanOrEqual(SERVER_CLIENT_TIMEOUT_MS)
  })

  it('waits for at least two of the server’s pings before giving the connection up', () => {
    expect(SERVER_TIMEOUT_MS).toBeGreaterThanOrEqual(SERVER_KEEP_ALIVE_MS * 2)
  })
})
