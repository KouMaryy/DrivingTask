# DrivingTask

A Unity-based driving simulation built to study how visual uncertainty and AI reliability affect human override behavior and reaction time in a semi-autonomous decision-making task. Developed as the experimental artifact for a Master's thesis in Informatics (User Experience Design), Jönköping University, 2026.

> **Thesis:** *Uncertainty, Imperfect AI, and Trust Calibration: Do We Decide Differently When We See Less?*
> A mixed-methods study of human override behavior under visual uncertainty in AI-assisted decision-making.

## Overview

The simulation places participants in the role of a supervisor of a semi-autonomous vehicle (Level 6 automation). On each trial, the AI assesses two oncoming obstacles : one safe and one dangerous. Then, it either maintains the current lane or switches lanes by default.
Participants have a fixed ~3.80-second window to accept the AI's decision or override it manually via keyboard input.
The AI is occasionally and deliberately unreliable, and visual conditions degrade across three weather levels, allowing the study to isolate how perceptual uncertainty and trust in automation jointly shape reliance behavior.

This repository contains the full Unity project used to run that experiment, including the trial-sequencing logic, the weather/fog manipulation, the AI decision and deception logic, and the automated behavioral data logger.

## Key Features

- **Three-level visual uncertainty manipulation:** Clear / Low Fog / Heavy Fog, implemented via dynamic fog and particle-based snow intensity.
- **Controllable AI reliability :** the AI is truthful on 75% of trials and deliberately deceptive on 25% (27 truthful / 9 deceptive trials per 36-trial session), to generate observable instances of both appropriate reliance and automation bias.
- **Level 6 supervisory control loop :** the AI executes its lane decision by default, the participant can intervene at any point before impact using the left/right arrow keys.
- **Latin Square trial counterbalancing :** three pre-generated trial sequences (Group1.csv, Group2.csv, Group3.csv) ensure weather conditions appear equally often across all serial positions, controlling for order effects.
- **Automated, per-trial CSV data logging :** every trial is logged with zero manual intervention, capturing reaction time, intervention count, AI/participant decisions, trial outcome, and timestamps, eliminating observer-induced bias.
- **In-car HUD and audio feedback :** minimal, single-message interface design (text + synthesized voice) intentionally kept simple to isolate the effect of uncertainty rather than information overload.
- **Built-in scoring system :** a dynamic point system that rewards correctly overriding the AI under uncertainty more than under clear conditions, reflecting the asymmetric real-world cost of automation bias vs. unnecessary intervention.

## Logged Data

Each participant session produces a CSV file (P01.csv–P30.csv in the study) with one row per trial:

| Field | Description |
|---|---|
| `Weather_Condition` | Clear / Low Fog / Heavy Fog |
| `AI_Lying` | Whether the AI's recommendation was deceptive (True/False) |
| `AI_Action` | Maintain / Switch |
| `Intervened` | Whether the participant overrode the AI (True/False) |
| `Num_Interventions` | Number of keypresses before the final lane choice |
| `Reaction_Time_Seconds` | Time from AI decision to participant intervention |
| `AI_Time_Stamp` / `Player_Time_Stamp` | Raw event timestamps |
| `AI_Suggested_Lane` / `Final_Lane` | Left / Right |
| `Success` | Whether the final lane was the safe one |
| `Total_Score` | Running cumulative score |
| `Dangerous_Object` / `Safe_Object` | Obstacle material types presented that trial |
| `Timestamp` | Wall-clock time of trial completion |

This structure allows each trial to be classified under a Signal Detection Theory framework (Hit / Miss / False Alarm / Correct Reliance) for downstream statistical analysis.

## Tech Stack

- Engine: Unity 6000.3.8f1
- Language: C#
- Audio: AI-generated voiceover (Artlist)
- Trial design: Latin Square counterbalancing, CSV-driven trial sequencing

## Getting Started

### Prerequisites

Unity Hub with Editor version 6000.3.8f1 installed


### Setup

```bash
git clone https://github.com/KouMaryy/DrivingTask.git
```

1. Open Unity Hub → Add project from disk → select the cloned DrivingTask folder.
2. Open the project with Unity 6000.3.8f1.
3. Open the main scene under Assets/Scenes/.
4. Press Play to run the simulation.

### Running a session

Trial sequences are pre-defined per participant group in Group1.csv, Group2.csv, and Group3.csv at the project root, each encoding 36 trials' worth of aiMessage, shouldGoLeft, aiIsLying, and weatherIntensity values. Assign a participant to a group to reproduce the exact counterbalanced sequence used in the original study (see 3.2.5 Trial Structure and Counterbalancing in the accompanying thesis for the full rationale).

Behavioral logs are written automatically to a participant-specific CSV after each session.

## Repository Structure

```
DrivingTask/
├── Assets/              # Scenes, scripts, prefabs, materials, audio
├── Packages/            # Unity package manifest
├── ProjectSettings/     # Unity project configuration
├── Group1.csv           # Trial sequence — counterbalancing Group 1
├── Group2.csv           # Trial sequence — counterbalancing Group 2
├── Group3.csv           # Trial sequence — counterbalancing Group 3
└── DrivingTask.slnx     # Solution file
```

## Background & Motivation

This simulation operationalizes a Level 6 automation scenario (Parasuraman, Sheridan, & Wickens, 2000), in which the system acts by default and the human retains a restricted veto window — a setup chosen specifically to study status quo bias in human-AI supervisory control. The weather manipulation and AI deception ratio were designed to create a controlled but ecologically meaningful gradient of decision difficulty, letting the study disentangle two behavioral signals that are often conflated in the human-automation interaction literature: how often people override automation, and how long it takes them to decide.

## Citation

If you use or reference this simulation, please cite:


> Ebsa, L., & Kourtakou, M. (2026). *Uncertainty, Imperfect AI, and Trust Calibration: Do We Decide Differently When We See Less?* Master's thesis, Jönköping University, School of Engineering.


## Authors

- **Maria Kourtakou** — [@KouMaryy](https://github.com/KouMaryy)
- **Lenssa Ebsa**

Developed as part of a Master's thesis in Informatics, specialization in User Experience Design, Jönköping University (2026), supervised by Neziha Akalin.

## License

This project was developed for academic research purposes. Please contact the authors before reuse in other research or commercial contexts.
