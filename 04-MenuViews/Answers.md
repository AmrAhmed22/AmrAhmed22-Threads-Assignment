# Assignment 04 — Answers

Student name: Amr  
Group / ID:G4

## Why Expected and Actual disagree

because the incrementing process is not atomic,two threads can read the same value and try to update it at the same time so we lost an update here.
we have 8 threads so we will lose so many increments


-

## What you changed

making the increment process atomic using Interlocked.increment()
in one line , no updates lost here
-

## When would you protect the update differently
if there was a condition i will use lock to ensure it's atomic or if there is more than one value to change

-
