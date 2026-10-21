import { expect, test } from '@playwright/test'
import { b1Candidate, b1ExamName, candidateSignsIn, endSectionNow, invigilator, staffSignsIn } from './accounts'

const examiner = { username: 'giamkhao', password: '19072005' }

/**
 * VE-38: a B1 Preliminary for Schools exam from start to finish — Reading, Writing (choose one of questions 2 and 3),
 * Listening (Part 2 one question per page) — ended by the invigilator, marked automatically, and the chosen Writing
 * question marked by the examiner to a final score.
 */
test('a B1 exam runs through Reading, Writing and Listening and ends with a final score', async ({ browser }) => {
  test.setTimeout(240_000)
  const candidateContext = await browser.newContext()
  const staffContext = await browser.newContext()
  const examinerContext = await browser.newContext()
  const screen = await candidateContext.newPage()
  const portal = await staffContext.newPage()
  const marking = await examinerContext.newPage()
  await staffSignsIn(portal, invigilator)

  // Reading: the three sections run in order.
  await candidateSignsIn(screen, b1Candidate)
  await expect(screen.getByRole('button', { name: 'Writing is locked' })).toBeDisabled()
  await expect(screen.getByRole('button', { name: 'Listening is locked' })).toBeDisabled()
  await screen.getByRole('button', { name: 'Open Reading' }).click()
  const saved = screen.waitForResponse((r) => r.url().includes('/responses') && r.request().method() === 'PUT' && r.status() === 204)
  await screen.getByRole('radiogroup', { name: 'Answers for question 1' }).getByRole('radio').first().check()
  await saved
  await endSectionNow(portal, b1ExamName, /Reading/, b1Candidate.fullName)
  await expect(screen.getByText('The test has now finished. Your answers have been saved.')).toBeVisible({ timeout: 10_000 })
  await screen.getByRole('button', { name: 'Continue' }).click()

  // Writing: Part 1 email, then Part 2 — the candidate chooses question 2, and Yes is no longer offered on question 3.
  await screen.getByRole('button', { name: 'Open Writing' }).click()
  await screen.getByRole('textbox', { name: 'Question 1' }).fill('Hi Alex, the morning is better because the cooking show starts at ten.')
  await screen.getByRole('button', { name: /^Part 2/ }).click()
  const choice2 = screen.getByRole('combobox', { name: 'Answering question 2?' })
  await expect(choice2).toHaveValue('Undecided')
  await expect(screen.getByText('0 of 1 question selected')).toBeVisible()
  const chosen = screen.waitForResponse((r) => r.url().includes('/selections') && r.status() === 204)
  await choice2.selectOption('Yes')
  await chosen
  await expect(screen.getByText('1 of 1 question selected')).toBeVisible()
  await screen.getByRole('textbox', { name: 'Question 2' }).fill('My favourite place is the beach near my grandparents’ house.')
  await screen.getByRole('button', { name: 'Question 3' }).click()
  await expect(screen.getByRole('textbox', { name: 'Question 2' })).toBeHidden()
  await expect(screen.getByRole('combobox', { name: 'Answering question 3?' }).getByRole('option', { name: 'Yes' })).toHaveJSProperty('disabled', true)
  await screen.waitForResponse((r) => r.url().includes('/responses') && r.status() === 204)
  await endSectionNow(portal, b1ExamName, /Writing/, b1Candidate.fullName)
  await expect(screen.getByText('The test has now finished. Your answers have been saved.')).toBeVisible({ timeout: 10_000 })
  await screen.getByRole('button', { name: 'Continue' }).click()

  // Listening: after Play, Part 2 shows one question per page.
  await screen.getByRole('button', { name: 'Open Listening' }).click()
  await screen.getByRole('dialog', { name: 'Listening' }).getByRole('button', { name: 'Play' }).click()
  await screen.getByRole('button', { name: /^Part 2/ }).click()
  await expect(screen.getByRole('radiogroup', { name: 'Answers for question 8' })).toBeVisible()
  await expect(screen.getByRole('radiogroup', { name: 'Answers for question 9' })).toBeHidden()
  await endSectionNow(portal, b1ExamName, /Listening/, b1Candidate.fullName)
  await expect(screen.getByText('The test has now finished. Your answers have been saved.')).toBeVisible({ timeout: 10_000 })

  // The examiner marks the email and the chosen question; question 3 was not chosen and is not marked.
  await staffSignsIn(marking, examiner)
  await marking.getByRole('link', { name: b1ExamName }).click()
  await marking.getByRole('link', { name: 'Results' }).click()
  const mark = marking.getByRole('link', { name: 'Mark Writing' }).first()
  await expect(mark).toBeVisible({ timeout: 20_000 })
  await mark.click()
  await expect(marking.getByText(/Not chosen by the candidate/)).toBeVisible()
  for (const number of [1, 2]) {
    const writing = marking.locator('section', { has: marking.getByRole('heading', { name: new RegExp(`Question ${number} ·`) }) })
    await writing.getByLabel(/Mark \(0–/).fill('12')
    await writing.getByRole('button', { name: 'Save mark' }).click()
    await expect(writing.getByText(/Saved/)).toBeVisible()
  }

  await expect(marking.getByText('24 / 40')).toBeVisible()
  await marking.getByRole('link', { name: 'Results' }).click()
  await expect(marking.getByText('Final').first()).toBeVisible()

  await candidateContext.close()
  await staffContext.close()
  await examinerContext.close()
})
