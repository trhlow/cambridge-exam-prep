import { expect, type Page } from '@playwright/test'

/** The demo accounts the Development stack creates (appsettings.Development.json, section Demo). */
export const candidate = { username: 'hoanglong', password: '19072005', pin: '4821', fullName: 'Hoàng Long Trần' }
export const admin = { username: 'hoanglong', password: '19072005' }
export const secondAdmin = { username: 'admin2', password: '19072005' }
export const manager = { username: 'quantri', password: '19072005' }
export const invigilator = { username: 'giamthi', password: 'victory-invig' }
export const examName = 'Victory demo – A2 Key for Schools'

export async function candidateSignsIn(page: Page) {
  await page.goto('/')
  await page.getByLabel('Username').fill(candidate.username)
  await page.getByLabel('Password').fill(candidate.password)
  await page.getByRole('button', { name: 'Log in' }).click()
  await page.getByLabel('Enter the PIN your invigilator gives you').fill(candidate.pin)
  await page.getByRole('button', { name: 'Start' }).click()
}

export async function staffSignsIn(page: Page, account: { username: string; password: string }) {
  await page.goto('/staff')
  await page.getByLabel('Username').fill(account.username)
  await page.getByLabel('Password').fill(account.password)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page.getByRole('navigation', { name: 'Staff portal' })).toBeVisible()
}
