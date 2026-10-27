import { expect, type Page } from '@playwright/test'

/** The demo accounts the Development stack creates (appsettings.Development.json, section Demo). */
export const candidate = { username: 'hoanglong', password: '19072005', pin: '4821', fullName: 'Hoàng Long Trần' }
export const admin = { username: 'hoanglong', password: '19072005' }
export const secondAdmin = { username: 'admin2', password: '19072005' }
export const manager = { username: 'quantri', password: '19072005' }
export const invigilator = { username: 'giamthi', password: 'victory-invig' }
export const examName = 'Victory demo – A2 Key for Schools'
/** VE-38: the B1 demo exam (Reading, Writing, Listening), each section a mix of the self-written B1 sample with itself. */
export const b1Candidate = { ...candidate, username: 'hoanglongb1' }
export const b1ExamName = 'Victory demo – B1 Preliminary for Schools'

/**
 * The candidate steps of the Cambridge exam day booklet: Login, Next, PIN, "Unlock and Select", Next, then the
 * readiness check (Confirm and the forward arrow), back to the dashboard.
 */
export async function candidateSignsIn(page: Page, account = candidate) {
  await page.goto('/')
  await page.getByLabel('Username').fill(account.username)
  await page.getByLabel('Password').fill(account.password)
  await page.getByRole('button', { name: 'Login' }).click()
  await page.getByRole('button', { name: 'Next' }).click()
  await page.getByLabel('PIN code').fill(account.pin)
  await page.getByRole('button', { name: 'Unlock and Select' }).click()
  await page.getByRole('button', { name: 'Next' }).click()
  await page.getByRole('button', { name: /^(Start|Watch again)$/ }).click()
  await page.getByRole('button', { name: 'Confirm' }).click()
  await page.getByRole('button', { name: 'Continue' }).click()
}

/** From the dashboard into a component: Open, read the instructions, "I confirm", "Start (name)". */
export async function openSection(page: Page, title: string) {
  await page.getByRole('button', { name: `Open ${title}` }).click()
  const start = page.getByRole('button', { name: `Start ${title}` })
  if (await start.isVisible().catch(() => false)) {
    await page.getByRole('button', { name: 'I confirm' }).click()
    await start.click()
  }
}

export async function staffSignsIn(page: Page, account: { username: string; password: string }) {
  await page.goto('/staff')
  await page.getByLabel('Username').fill(account.username)
  await page.getByLabel('Password').fill(account.password)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page.getByRole('navigation', { name: 'Staff portal' })).toBeVisible()
}

/** The invigilator ends one section for the candidate now (Edit end time → End now, confirmed: it cannot be undone). */
export async function endSectionNow(portal: Page, exam: string, section: RegExp, fullName: string) {
  await portal.goto('/staff')
  await portal.getByRole('link', { name: exam }).click()
  await portal.getByRole('link', { name: section }).click()
  const row = portal.getByRole('row', { name: new RegExp(fullName) })
  await expect(row.getByText('Online')).toBeVisible({ timeout: 20_000 })
  await row.getByRole('checkbox', { name: `Select ${fullName}` }).check()
  await portal.getByRole('toolbar', { name: 'Actions for the selected candidates' }).getByRole('button', { name: 'Edit end time' }).click()
  const dialog = portal.getByRole('dialog')
  await dialog.getByRole('radio', { name: 'End now' }).check()
  await dialog.getByRole('button', { name: 'End now' }).click()
  await dialog.getByRole('button', { name: 'Click again to end now' }).click()
  await expect(row.getByText('Submitted')).toBeVisible()
}
