import { expect, test } from '@playwright/test'
import { admin, b1ExamName, staffSignsIn } from './accounts'

const PER_ROW = 4

/**
 * VE-68: the B1 sections deliver mixed papers by seat. With eight computers in two rows of four, no two neighbouring
 * computers — side by side, front and back, or diagonal — get the same version.
 */
test('neighbouring computers in the B1 room never get the same version', async ({ page }) => {
  await staffSignsIn(page, admin)
  await page.getByRole('link', { name: b1ExamName }).click()

  // Seats 2–8 for seven more candidates; the demo candidate (no seat number) sits at the first computer.
  await page.getByRole('button', { name: 'Add candidates' }).click()
  const rows = ['Le\tMai', 'Pham\tBinh', 'Vo\tChi', 'Do\tDung', 'Ho\tGiang', 'Ly\tHanh', 'Ta\tKhoa'].map((name, i) => `${String(i + 2).padStart(4, '0')}\t${name}\t\t${i + 2}`)
  await page.getByRole('textbox', { name: 'Candidate rows' }).fill(rows.join('\n'))
  await page.getByRole('button', { name: 'Add 7' }).click()
  await expect(page.getByRole('heading', { name: /Candidates \(8\)/ })).toBeVisible()

  const [plan] = await Promise.all([page.waitForEvent('popup'), page.getByRole('link', { name: 'Room plan' }).click()])
  const versions = new Map<number, string>()
  for (let seat = 1; seat <= 8; seat++) {
    const computer = plan.getByRole('listitem', { name: `Computer ${seat}` })
    versions.set(seat, (await computer.locator('.room-version').first().textContent()) ?? '')
  }

  expect(new Set(versions.values())).toEqual(new Set(['V1', 'V2', 'V3', 'V4']))
  for (const [seat, version] of versions) {
    const row = Math.floor((seat - 1) / PER_ROW)
    const column = (seat - 1) % PER_ROW
    for (const [other, otherVersion] of versions) {
      const near = other !== seat && Math.abs(Math.floor((other - 1) / PER_ROW) - row) <= 1 && Math.abs(((other - 1) % PER_ROW) - column) <= 1
      if (near) {
        expect(otherVersion, `computers ${seat} and ${other}`).not.toBe(version)
      }
    }
  }
})
