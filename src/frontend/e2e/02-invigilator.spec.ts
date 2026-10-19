import { expect, test } from '@playwright/test'
import { candidate, candidateSignsIn, examName, invigilator, staffSignsIn } from './accounts'

test('the invigilator pauses and resumes a candidate, and the candidate screen follows at once', async ({ browser }) => {
  const candidateContext = await browser.newContext()
  const staffContext = await browser.newContext()
  const screen = await candidateContext.newPage()
  const portal = await staffContext.newPage()

  await candidateSignsIn(screen)
  await screen.getByRole('button', { name: 'Open Reading & Writing' }).click()
  await expect(screen.getByRole('radiogroup', { name: 'Answers for question 1' })).toBeVisible()

  await staffSignsIn(portal, invigilator)
  await portal.getByRole('link', { name: examName }).click()
  await portal.getByRole('link', { name: /Reading & Writing/ }).click()
  await expect(portal.getByText('live')).toBeVisible()

  const row = portal.getByRole('row', { name: new RegExp(candidate.fullName) })
  await expect(row.getByText('Online')).toBeVisible({ timeout: 20_000 })
  await row.getByRole('checkbox', { name: `Select ${candidate.fullName}` }).check()

  const actions = portal.getByRole('toolbar', { name: 'Actions for the selected candidates' })
  await actions.getByRole('button', { name: 'Pause' }).click()
  await portal.getByRole('button', { name: 'Submit event' }).click()
  await expect(screen.getByRole('alertdialog', { name: 'Test paused' })).toBeVisible({ timeout: 5_000 })
  await expect(row.getByText('Paused')).toBeVisible()

  await actions.getByRole('button', { name: 'Resume' }).click()
  await portal.getByRole('dialog').getByRole('button', { name: 'Resume' }).click()
  await expect(screen.getByRole('alertdialog', { name: 'Test paused' })).toBeHidden({ timeout: 5_000 })

  await candidateContext.close()
  await staffContext.close()
})
