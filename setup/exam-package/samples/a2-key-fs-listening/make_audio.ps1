# Records the synthetic A2 Listening sample with the Windows speech voices (David, Zira), one mp3 per part,
# each extract played twice as in Cambridge Listening tests. The script is our own, written to match the
# answers in paper.json. Needs Windows PowerShell 5.1 (System.Speech) and ffmpeg on PATH.
#   powershell -NoProfile -ExecutionPolicy Bypass -File setup/exam-package/samples/a2-key-fs-listening/make_audio.ps1

Add-Type -AssemblyName System.Speech
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$audioDir = Join-Path $here 'audio'
New-Item -ItemType Directory -Force -Path $audioDir | Out-Null

$M = 'Microsoft David Desktop'
$F = 'Microsoft Zira Desktop'
$N = $M   # narrator

function Say($voice, $text) { @{ Voice = $voice; Text = $text } }

$parts = [ordered]@{
  part1 = @{
    Intro = 'Part 1. For each question, choose the correct answer. Look at the three pictures for question one now.'
    Extracts = @(
      @( (Say $N 'One. What will the weather be like tomorrow?'),
         (Say $F 'Is it going to be sunny for the picnic tomorrow?'),
         (Say $M 'I am afraid not. The radio says it will rain all day. At least it will not be windy.') ),
      @( (Say $N 'Two. What did the boy buy at the shop?'),
         (Say $F 'Did you get a new T-shirt in town?'),
         (Say $M 'No, they were too expensive. I bought a cap instead. I still need a bag for school, though.') ),
      @( (Say $N 'Three. Where are the girl''s keys?'),
         (Say $F 'Dad, I cannot find my keys. They are not on the table.'),
         (Say $M 'Did you look in your coat?'),
         (Say $F 'Yes, they are not there either.'),
         (Say $M 'Then look under the sofa. That is where they were last time.'),
         (Say $F 'Oh, here they are!') ),
      @( (Say $N 'Four. What time does the film start?'),
         (Say $M 'Shall we meet at quarter past six?'),
         (Say $F 'That is a bit early. The film starts at six forty-five, so let us meet at half past six. The last bus home is at seven thirty, so we must not be late.') ),
      @( (Say $N 'Five. Which sport will the children play this afternoon?'),
         (Say $F 'It is too windy for volleyball outside, and the basketball court is closed today, so this afternoon we will play tennis in the sports hall.') )
    )
  }
  part2 = @{
    Intro = 'Part 2. For each question, write the correct answer in the gap. Write one word or a number or a date or a time. You will hear a teacher talking to students about a school trip.'
    Extracts = @(
      @( (Say $F 'Listen carefully, everyone. Next month we are going on a trip to the science museum in Bristol. That is B, R, I, S, T, O, L. The trip is on the fourteenth of November. The bus leaves school at eight fifteen, so please do not be late. The trip costs twelve pounds, and that includes the ticket for the planetarium. Please bring a packed lunch, and do not forget a notebook, because you will need to write about what you see.') )
    )
  }
  part3 = @{
    Intro = 'Part 3. For each question, choose the correct answer. You will hear Emma talking to her friend Jack about a holiday.'
    Extracts = @(
      @( (Say $M 'Hi Emma. How was your holiday? Did you go to the beach again?'),
         (Say $F 'No, this year we stayed on a farm in the hills. We went there by train, which took four hours, but it was fun.'),
         (Say $M 'Did you ride the horses?'),
         (Say $F 'Only once. What I loved most was feeding the animals every morning, especially the baby goats.'),
         (Say $M 'Was the weather good?'),
         (Say $F 'It was sunny, but it was really windy every day, so we always needed a jacket.'),
         (Say $M 'What about next summer?'),
         (Say $F 'We have already booked the same farm. I cannot wait to go back.') )
    )
  }
  part4 = @{
    Intro = 'Part 4. For each question, choose the correct answer.'
    Extracts = @(
      @( (Say $N 'Sixteen. You will hear a girl talking about her new school. What does she like about it?'),
         (Say $F 'The teachers are nice and the lunches are fine, but the best thing is the library. It is huge, and I go there every break.') ),
      @( (Say $N 'Seventeen. You will hear a boy talking to his mother. What does he want to do?'),
         (Say $M 'Mum, can I go to Sam''s house after school? We are not going to watch the match or play computer games, I promise. We just want to finish our project together.') ),
      @( (Say $N 'Eighteen. You will hear a teacher talking to a class. Why is the lesson different today?'),
         (Say $F 'There is no test today, and it is raining, so we cannot go outside. Instead, we are going to watch a short video about volcanoes.') ),
      @( (Say $N 'Nineteen. You will hear two friends talking about a book. What did the boy think of it?'),
         (Say $F 'Did you like the book? It is very long.'),
         (Say $M 'It is, and some words were difficult, but it was so funny. I laughed all the time.') ),
      @( (Say $N 'Twenty. You will hear a girl talking about a sports club. When does the club meet?'),
         (Say $F 'The club used to meet on Monday evenings and Wednesday afternoons, but now we only meet on Saturday mornings.') )
    )
  }
  part5 = @{
    Intro = 'Part 5. For each question, choose the correct answer. You will hear Leo talking to his aunt about a family picnic. What will each person bring to the picnic?'
    Extracts = @(
      @( (Say $F 'So, Leo, is everything ready for the picnic on Sunday?'),
         (Say $M 'Nearly. Grandma is making a big chocolate cake.'),
         (Say $F 'Lovely. And Uncle Rob?'),
         (Say $M 'He wanted to bring games, but we have lots, so he is bringing his speaker for music.'),
         (Say $F 'What about Lily?'),
         (Say $M 'Lily is going to bring fruit. She is buying strawberries at the market.'),
         (Say $F 'And Mark?'),
         (Say $M 'Mark has a big blanket for everyone to sit on. He forgot the plates last time, so I am bringing those.'),
         (Say $F 'And your dad?'),
         (Say $M 'Dad is making the sandwiches. He says he will bring drinks too, but I am not sure.') )
    )
  }
}

function Record($name, $part) {
  $wav = Join-Path $env:TEMP "victory-$name.wav"
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
  & ffmpeg -loglevel error -y -i $wav -ac 1 -ar 22050 -b:a 48k $mp3
  Remove-Item $wav
  Write-Host "Wrote $mp3"
}

foreach ($name in $parts.Keys) { Record $name $parts[$name] }
