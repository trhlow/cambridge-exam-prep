# Records the synthetic B1 Listening sample with the Windows speech voices (David, Zira), one mp3 per part. As in
# Cambridge B1 Listening, every extract (Parts 1-2) and every whole recording (Parts 3-4) is played twice. The script
# is our own, written to match the answers in paper.json. Needs Windows PowerShell 5.1 (System.Speech) and ffmpeg
# (PATH, or the FFMPEG environment variable).
#   powershell -NoProfile -ExecutionPolicy Bypass -File setup/exam-package/samples/b1-pfs-listening/make_audio.ps1

Add-Type -AssemblyName System.Speech
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$audioDir = Join-Path $here 'audio'
New-Item -ItemType Directory -Force -Path $audioDir | Out-Null
$ffmpeg = if ($env:FFMPEG) { $env:FFMPEG } else { 'ffmpeg' }

$M = 'Microsoft David Desktop'
$F = 'Microsoft Zira Desktop'
$N = $M   # narrator

function Say($voice, $text) { @{ Voice = $voice; Text = $text } }

$parts = [ordered]@{
  part1 = @{
    Intro = 'Part 1. For each question, choose the correct answer. Look at the three pictures for question one now.'
    Extracts = @(
      @( (Say $N 'One. What time does the film start?'),
         (Say $F 'Shall we meet at seven? The film starts at eight, doesn''t it?'),
         (Say $M 'Not any more. I checked online and it starts at half past seven now. So seven is fine for meeting.') ),
      @( (Say $N 'Two. What will the boy take on the trip?'),
         (Say $M 'Should I take my camera tomorrow?'),
         (Say $F 'Your phone takes good photos. But it might rain, so take an umbrella. And don''t worry about a map, the teacher has got some.') ),
      @( (Say $N 'Three. Which sport does the girl do now?'),
         (Say $M 'Do you still go swimming every morning?'),
         (Say $F 'Not since last year. Now I play tennis with my sister after school. I''d like to try football one day, though.') ),
      @( (Say $N 'Four. What will the weather be like on Sunday?'),
         (Say $F 'Here is the weather for the weekend. Saturday will be wet, with rain all day. On Sunday the rain will stop and it will be sunny all day. It may get a little windy on Monday.') ),
      @( (Say $N 'Five. Where is the boy''s phone?'),
         (Say $M 'I thought I put my phone in my bag, but it isn''t there.'),
         (Say $F 'Is that it on your desk?'),
         (Say $M 'No, that''s my calculator. Oh, I remember! I left it in the kitchen, next to the fridge.') ),
      @( (Say $N 'Six. How much did the girl pay for her jacket?'),
         (Say $M 'I love your new jacket. Was it expensive?'),
         (Say $F 'It was forty pounds, but it was in the sale, so I only paid twenty-five. My new shoes were just fifteen.') ),
      @( (Say $N 'Seven. What will the class do on Friday afternoon?'),
         (Say $F 'We were going to visit the museum on Friday afternoon, but it''s closed for repairs. So instead we''ll plant some trees in the school garden. The trip to watch the play is still next month.') )
    )
  }
  part2 = @{
    Intro = 'Part 2. For each question, choose the correct answer.'
    Extracts = @(
      @( (Say $N 'Question eight. You will hear two friends talking about a new sports centre.'),
         (Say $F 'Have you been to the new sports centre? It''s quite expensive, isn''t it?'),
         (Say $M 'Yes, and it closes too early. But the machines in the gym are brilliant. They''re all brand new.') ),
      @( (Say $N 'Question nine. You will hear a girl telling her friend about a school concert.'),
         (Say $F 'I''d practised for weeks, so I wasn''t tired at all. But just before we went on stage, my hands were shaking. Once I started playing, I loved it.') ),
      @( (Say $N 'Question ten. You will hear two friends talking about a book they have read.'),
         (Say $M 'I thought it was far too long.'),
         (Say $F 'Really? I didn''t. But I never guessed what would happen at the end.'),
         (Say $M 'Me neither! That was the best part.') ),
      @( (Say $N 'Question eleven. You will hear a boy talking to his mother about a weekend job.'),
         (Say $F 'Why do you want to work at the café? To meet new people?'),
         (Say $M 'Not really. Dan''s family have invited me to Spain in the summer, and I need to pay for my flight.') ),
      @( (Say $N 'Question twelve. You will hear two friends talking about a cooking lesson.'),
         (Say $M 'How was the cooking lesson?'),
         (Say $F 'The teacher arrived on time and there was plenty of food. But there were twenty of us in that tiny kitchen. We kept bumping into each other.') ),
      @( (Say $N 'Question thirteen. You will hear a girl talking about a trip to the mountains.'),
         (Say $F 'The walking was really hard, and the food was just sandwiches. But when we got to the top, we could see three lakes and the sea. That was the best thing about the whole trip.') )
    )
  }
  part3 = @{
    Intro = 'Part 3. For each question, write the correct answer in the gap. Write one or two words or a number or a date or a time. You will hear a teacher talking about a summer art course. You now have twenty seconds to look at Part 3.'
    Extracts = @(
      ,@( (Say $F 'Good morning, everyone. I''d like to tell you about this year''s summer art course. Last year we met in the museum café, but this year the course takes place in the garden of the town museum, so we''ll be outside when the weather is good.'),
          (Say $F 'We won''t use ordinary paints. Instead, students will learn to paint using coffee. It gives lovely brown colours, and it smells nice too.'),
          (Say $F 'The course isn''t ten days long like last year. It lasts for twelve days, from Monday the third of August.'),
          (Say $F 'Painting can be messy, so each student should bring an old shirt to wear. You don''t need to bring brushes. We have plenty.'),
          (Say $F 'At the end of the course, your paintings will be shown in a bookshop in the High Street, not in the library as before.'),
          (Say $F 'To book a place, phone Mrs Pearce. That''s P, E, A, R, C, E. Her number is on the school website.') )
    )
  }
  part4 = @{
    Intro = 'Part 4. For each question, choose the correct answer. You will hear an interview with a young chess player called Tom Reed. You now have forty-five seconds to look at Part 4.'
    Extracts = @(
      ,@( (Say $F 'Tom, how did you start playing chess?'),
          (Say $M 'There was a chess club at my school, but I didn''t join it. My grandfather taught me when I was six. We played every Sunday, and later I watched games online too.'),
          (Say $F 'What do you find most difficult in competitions?'),
          (Say $M 'I don''t usually get nervous, and I love travelling to new places. The hardest thing is that games can go on for five or six hours. That''s really tiring.'),
          (Say $F 'How do you prepare for an important game?'),
          (Say $M 'Some players practise against a computer, but I don''t. I study famous games from the past. You can learn so much from them.'),
          (Say $F 'And your friends? Do they play chess?'),
          (Say $M 'Not really, but that''s fine. When I''m with them, I forget about chess and just relax. They''re great.'),
          (Say $F 'What are your plans for next year?'),
          (Say $M 'People say I should write a book or teach younger children, and maybe I will one day. But next year I''m going to play in a competition in Canada.'),
          (Say $F 'Finally, any advice for new players?'),
          (Say $M 'You don''t have to practise every day. Just don''t worry when you lose. Everybody loses, and every game teaches you something.') )
    )
  }
}

function Record($name, $part) {
  $wav = Join-Path $env:TEMP "victory-b1-$name.wav"
  $mp3 = Join-Path $audioDir "$name.mp3"
  $builder = New-Object System.Speech.Synthesis.PromptBuilder
  $builder.StartVoice($N); $builder.AppendText($part.Intro); $builder.EndVoice()
  $builder.AppendBreak([TimeSpan]::FromSeconds(3))
  foreach ($extract in $part.Extracts) {
    foreach ($round in 1..2) {
      if ($round -eq 2) { $builder.StartVoice($N); $builder.AppendText('Now listen again.'); $builder.EndVoice(); $builder.AppendBreak([TimeSpan]::FromSeconds(1)) }
      foreach ($line in $extract) {
        $builder.StartVoice($line.Voice); $builder.AppendText($line.Text); $builder.EndVoice()
        $builder.AppendBreak([TimeSpan]::FromMilliseconds(600))
      }
      $builder.AppendBreak([TimeSpan]::FromSeconds(3))
    }
  }
  $builder.StartVoice($N); $builder.AppendText('That is the end of this part.'); $builder.EndVoice()

  $synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
  $synth.Rate = -1
  $synth.SetOutputToWaveFile($wav)
  $synth.Speak($builder)
  $synth.Dispose()
  & $ffmpeg -loglevel error -y -i $wav -ac 1 -ar 22050 -b:a 48k $mp3
  Remove-Item $wav
  Write-Host "Wrote $mp3"
}

foreach ($name in $parts.Keys) { Record $name $parts[$name] }
