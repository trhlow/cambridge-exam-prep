"""Finding the part boundaries of a whole Listening recording from its pauses."""

import pytest

from split_listening import part_boundaries

# (end, duration) of the pauses ffmpeg found in the official A2 Key for Schools 2022 sample recording.
# Cambridge's own part tracks for it end at 359.2, 598.0, 869.7 and 1305.4 seconds.
A2_KEY_FOR_SCHOOLS = [
    (32.8, 5.3), (40.8, 5.3), (53.1, 5.3), (82.3, 5.4), (107.5, 5.3), (136.0, 5.4), (160.2, 5.3), (196.0, 5.4),
    (227.8, 5.4), (260.9, 5.4), (289.3, 5.4), (320.7, 5.4), (348.2, 5.4), (360.4, 10.4), (367.7, 5.4),
    (395.6, 10.4), (493.0, 5.4), (587.1, 5.3), (599.3, 10.3), (606.7, 5.4), (639.3, 20.9), (750.8, 5.4),
    (858.6, 5.4), (871.1, 10.4), (878.6, 5.4), (887.0, 5.4), (932.2, 5.3), (968.8, 5.4), (1009.8, 5.4),
    (1043.2, 5.4), (1092.3, 5.4), (1131.5, 5.5), (1176.2, 5.4), (1211.8, 5.4), (1257.3, 5.5), (1294.3, 5.5),
    (1306.7, 10.4), (1314.4, 5.5), (1341.0, 15.3), (1423.5, 5.4), (1499.4, 5.4), (1808.5, 300.3), (1872.6, 60.2),
]


def test_cuts_where_cambridge_cut_the_a2_key_for_schools_sample():
    cuts = part_boundaries(A2_KEY_FOR_SCHOOLS, parts=5)

    assert cuts == pytest.approx([359.4, 598.3, 870.1, 1305.7], abs=0.05)
    for cut, official in zip(cuts, [359.2, 598.0, 869.7, 1305.4]):
        assert abs(cut - official) < 0.5


def test_a_reading_pause_without_an_announcement_after_it_is_not_a_boundary():
    # 395.6 (10 s to read Part 2) is followed by speech, not by "Now look at Part n".
    assert 394.6 not in part_boundaries(A2_KEY_FOR_SCHOOLS, parts=5)


def test_refuses_when_the_count_does_not_match_the_parts():
    with pytest.raises(ValueError, match="found 4 part boundaries, expected 3"):
        part_boundaries(A2_KEY_FOR_SCHOOLS, parts=4)


def test_refuses_a_recording_without_the_cambridge_pauses():
    with pytest.raises(ValueError, match="found 0 part boundaries.*none"):
        part_boundaries([(10.0, 3.2), (50.0, 4.1)], parts=5)
