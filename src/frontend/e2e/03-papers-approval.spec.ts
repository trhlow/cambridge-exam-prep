import { expect, test } from '@playwright/test'
import { manager, secondAdmin, staffSignsIn } from './accounts'

test('a manager starts a blank paper, cannot publish it unwritten, and its deletion waits for an administrator', async ({ browser }) => {
  const title = `E2E blank ${Date.now()}`
  const managerContext = await browser.newContext()
  const adminContext = await browser.newContext()
  const portal = await managerContext.newPage()
  const approver = await adminContext.newPage()

  await staffSignsIn(portal, manager)
  await portal.getByRole('link', { name: 'Papers' }).click()
  await portal.getByRole('button', { name: 'New blank paper' }).click()
  await portal.getByLabel('Title').fill(title)
  await portal.getByRole('button', { name: 'Create draft' }).click()

  await expect(portal.getByRole('heading', { name: title })).toBeVisible()
  await expect(portal.getByText(/\[to write/).first()).toBeVisible()
  await portal.getByRole('button', { name: 'Publish' }).click()
  await expect(portal.getByText('The paper still has places to write.')).toBeVisible()

  await portal.getByRole('link', { name: 'Papers' }).first().click()
  const row = portal.getByRole('row', { name: new RegExp(title) })
  await row.getByRole('button', { name: 'Request deletion' }).click()
  await row.getByRole('button', { name: 'Ask an administrator to delete' }).click()
  await expect(portal.getByText('Sent for approval:')).toBeVisible()
  await expect(row).toBeVisible()

  await staffSignsIn(approver, secondAdmin)
  await approver.getByRole('link', { name: 'Approvals' }).click()
  const request = approver.getByRole('row', { name: new RegExp(title) })
  await request.getByRole('button', { name: 'Approve' }).click()
  await request.getByRole('button', { name: 'Click again to approve' }).click()

  await approver.getByRole('button', { name: /^All/ }).click()
  await expect(approver.getByRole('row', { name: new RegExp(title) }).getByText('Approved')).toBeVisible()
  await portal.reload()
  await expect(portal.getByRole('row', { name: new RegExp(title) })).toHaveCount(0)

  await managerContext.close()
  await adminContext.close()
})
