import { ApiError, candidateApi, request } from './client'

describe('request', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('exposes the code the server puts on a problem', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ title: 'Your test is paused.', code: 'paused' }), { status: 409 })))

    const error = await request('/x').catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(409)
    expect((error as ApiError).code).toBe('paused')
  })

  it('has no code when the server sent none', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ title: 'No.' }), { status: 400 })))

    const error = await request('/x').catch((e: unknown) => e)

    expect((error as ApiError).code).toBeUndefined()
  })
})

describe('candidateApi.saveResponsesOnPageHide', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('sends the answers with keepalive so the browser finishes the request after the page is gone', () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    candidateApi.saveResponsesOnPageHide(6, [{ questionNumber: 1, value: 'B' }])

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/candidate/sections/6/responses',
      expect.objectContaining({ method: 'PUT', keepalive: true, body: JSON.stringify({ responses: [{ questionNumber: 1, value: 'B' }] }) }),
    )
  })
})
