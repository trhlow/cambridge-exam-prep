import { createMonitorLoader } from './monitorLoader'

/** A fetch the test settles by hand, so it decides when each response arrives. */
function manualFetch() {
  const pending: ((value: string) => void)[] = []
  return {
    fetch: () => new Promise<string>((resolve) => pending.push(resolve)),
    settle: async (index: number, value: string) => {
      pending[index](value)
      await Promise.resolve()
      await Promise.resolve()
    },
    count: () => pending.length,
  }
}

describe('createMonitorLoader', () => {
  it('keeps one load in flight and loads once more afterwards when told of a change meanwhile', async () => {
    const source = manualFetch()
    const views: string[] = []
    const loader = createMonitorLoader(source.fetch, (v) => views.push(v), () => undefined)

    loader.request()
    loader.request()
    loader.request()
    expect(source.count()).toBe(1)

    await source.settle(0, 'first')
    expect(source.count()).toBe(2)
    await source.settle(1, 'second')

    expect(views).toEqual(['first', 'second'])
    expect(source.count()).toBe(2)
  })

  it('does not load again when nothing changed during a load', async () => {
    const source = manualFetch()
    const loader = createMonitorLoader(source.fetch, () => undefined, () => undefined)

    loader.request()
    await source.settle(0, 'only')
    loader.request()

    expect(source.count()).toBe(2)
  })

  it('reports a failed load and still loads again when a change arrived meanwhile', async () => {
    const errors: unknown[] = []
    let calls = 0
    const loader = createMonitorLoader(
      () => (++calls === 1 ? Promise.reject(new Error('offline')) : Promise.resolve('back')),
      () => undefined,
      (e) => errors.push(e),
    )

    loader.request()
    loader.request()
    await new Promise((resolve) => setTimeout(resolve, 0))

    expect(errors).toHaveLength(1)
    expect(calls).toBe(2)
  })

  it('shows nothing once disposed', async () => {
    const source = manualFetch()
    const views: string[] = []
    const loader = createMonitorLoader(source.fetch, (v) => views.push(v), () => undefined)

    loader.request()
    loader.dispose()
    await source.settle(0, 'late')

    expect(views).toEqual([])
  })
})
