import { parseRoute } from './router'

describe('parseRoute', () => {
  it('maps staff portal addresses to pages', () => {
    expect(parseRoute('/staff')).toEqual({ page: 'exams' })
    expect(parseRoute('/staff/exams/new')).toEqual({ page: 'new-exam' })
    expect(parseRoute('/staff/exams/12')).toEqual({ page: 'exam', examId: 12 })
    expect(parseRoute('/staff/exams/12/edit')).toEqual({ page: 'edit-exam', examId: 12 })
    expect(parseRoute('/staff/exams/12/sections/31')).toEqual({ page: 'monitor', examId: 12, sectionId: 31 })
    expect(parseRoute('/staff/exams/12/slips')).toEqual({ page: 'slips', examId: 12 })
    expect(parseRoute('/staff/papers')).toEqual({ page: 'papers' })
    expect(parseRoute('/staff/papers/5')).toEqual({ page: 'paper', paperId: 5 })
    expect(parseRoute('/staff/exams/12/marking/77')).toEqual({ page: 'marking', examId: 12, attemptId: 77 })
  })

  it('rejects anything else', () => {
    expect(parseRoute('/staff/exams/abc')).toEqual({ page: 'not-found' })
    expect(parseRoute('/staff/exams/12/sections/x')).toEqual({ page: 'not-found' })
    expect(parseRoute('/staff/secret')).toEqual({ page: 'not-found' })
    expect(parseRoute('/staff/papers/x')).toEqual({ page: 'not-found' })
  })
})
