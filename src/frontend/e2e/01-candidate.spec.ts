import { expect, test } from '@playwright/test'
import { candidateSignsIn } from './accounts'

test('a candidate signs in with the PIN, answers, and finds the answer again after a reload', async ({ page }) => {
  await candidateSignsIn(page)

  await expect(page.getByRole('button', { name: 'Listening is locked' })).toBeDisabled()
  await page.getByRole('button', { name: 'Open Reading & Writing' }).click()

  const answers = page.getByRole('radiogroup', { name: 'Answers for question 1' })
  const saved = page.waitForResponse((r) => r.url().includes('/responses') && r.request().method() === 'PUT' && r.status() === 204)
  await answers.getByRole('radio').nth(1).check()
  await saved

  await page.reload()
  await expect(page.getByRole('radiogroup', { name: 'Answers for question 1' }).getByRole('radio').nth(1)).toBeChecked()
})
