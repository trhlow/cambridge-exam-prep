import { expect, test } from '@playwright/test'
import { candidate, candidateSignsIn, examName, invigilator, staffSignsIn, openSection } from './accounts'

const examiner = { username: 'giamkhao', password: '19072005' }

test('the invigilator ends the section, it is marked automatically, and the examiner marks Writing to a final score', async ({ browser }) => {
  const candidateContext = await browser.newContext()
  const staffContext = await browser.newContext()
  const examinerContext = await browser.newContext()
  const screen = await candidateContext.newPage()
  const portal = await staffContext.newPage()
  const marking = await examinerContext.newPage()

  await candidateSignsIn(screen)
  await openSection(screen, 'Reading & Writing')
  await expect(screen.getByRole('radiogroup', { name: 'Answers for question 1' })).toBeVisible()

  // The invigilator ends the section now (Edit end time → End now, two clicks: it cannot be undone).
  await staffSignsIn(portal, invigilator)
  await portal.getByRole('link', { name: examName }).click()
  await portal.getByRole('link', { name: /Reading & Writing/ }).click()
  const row = portal.getByRole('row', { name: new RegExp(candidate.fullName) })
  await expect(row.getByText('Online')).toBeVisible({ timeout: 20_000 })
  await row.getByRole('checkbox', { name: `Select ${candidate.fullName}` }).check()
  await portal.getByRole('toolbar', { name: 'Actions for the selected candidates' }).getByRole('button', { name: 'Edit end time' }).click()
  const dialog = portal.getByRole('dialog')
  await dialog.getByRole('radio', { name: 'End now' }).check()
  await dialog.getByRole('button', { name: 'End now' }).click()
  await dialog.getByRole('button', { name: 'Click again to end now' }).click()

  // The candidate's screen ends at once, and Listening opens.
  await expect(screen.getByText('The test has now finished. Your answers have been saved.')).toBeVisible({ timeout: 10_000 })
  await expect(row.getByText('Submitted')).toBeVisible()

  // Reading is marked automatically; the Writing is dealt to the examiner, who sees it by code only (VE-73).
  await staffSignsIn(marking, examiner)
  await expect(marking.getByRole('heading', { name: 'Marking' })).toBeVisible()
  const mark = marking.getByRole('row', { name: new RegExp(examName) }).getByRole('link', { name: 'Mark Writing' }).first()
  // The marker runs every 5 s after the section ends; the list shows what was dealt when it loaded.
  await expect(async () => {
    await marking.reload()
    await expect(mark).toBeVisible({ timeout: 2_000 })
  }).toPass({ timeout: 30_000 })
  await mark.click()
  await expect(marking.getByRole('heading', { name: /^Script [A-HJ-NP-Z2-9]{6} · Reading & Writing$/ })).toBeVisible()
  await expect(marking.getByText(candidate.fullName)).toHaveCount(0)

  for (const number of [31, 32]) {
    const writing = marking.locator('section', { has: marking.getByRole('heading', { name: new RegExp(`Question ${number} ·`) }) })
    await writing.getByLabel(/Mark \(0–/).fill('3')
    await writing.getByRole('button', { name: 'Save mark' }).click()
    await expect(writing.getByText(/Saved/)).toBeVisible()
  }

  await expect(marking.getByText('Final').first()).toBeVisible()

  await candidateContext.close()
  await staffContext.close()
  await examinerContext.close()
})
