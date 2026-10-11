import { parseCandidateRows } from './candidateRows'

describe('parseCandidateRows', () => {
  it('reads rows copied from Excel (tab separated) and skips a header row', () => {
    const text = 'Candidate number\tFamily name\tGiven name\tExtra time %\tSeat\n0101\tNguyễn\tAn\t25\tA1\n0102\tLê\tThị Hoa\t\t\n'

    expect(parseCandidateRows(text)).toEqual({
      rows: [
        { candidateNumber: '0101', lastName: 'Nguyễn', firstName: 'An', plannedExtraPercent: 25, seatNo: 'A1' },
        { candidateNumber: '0102', lastName: 'Lê', firstName: 'Thị Hoa', plannedExtraPercent: 0, seatNo: null },
      ],
      errors: [],
    })
  })

  it('also reads comma-separated (CSV) lines and ignores blank lines', () => {
    const { rows, errors } = parseCandidateRows('0201, Tran, Binh\r\n\r\n0202,Pham,Cuong,50\r\n')

    expect(errors).toEqual([])
    expect(rows.map((r) => [r.candidateNumber, r.lastName, r.firstName, r.plannedExtraPercent])).toEqual([
      ['0201', 'Tran', 'Binh', 0],
      ['0202', 'Pham', 'Cuong', 50],
    ])
  })

  it('reports lines with too few columns or a bad extra-time value by line number', () => {
    const { rows, errors } = parseCandidateRows('0301\tVo\n0302\tDo\tEm\tlots')

    expect(rows).toEqual([])
    expect(errors).toEqual(['Line 1: expected candidate number, family name and given name.', 'Line 2: extra time must be a whole number from 0 to 100.'])
  })
})
