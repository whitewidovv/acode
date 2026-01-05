docs\FINAL_PASS_TASK_REMEDIATION.md
docs\PROMPT_TO_EXPAND_TASK.md
CLAUDE.md  

we are currently expanding tasks from FINAL_PASS_TASK_REMEDIATION. the flow is to claim a task suite by replacing all of the task suite's [ ] with hourglass emoji, and as you work through and move to a task, replace the line instructions telling you to read the top of the file, the prompt to expand the task, and the claude.md file to refresh them in context (and do so, don't just remove the instructions and ignore them; you removing them is acknowledging that you see them and are doing as it says). replace the instruction line with in progress, then begin expansion as described in those files.   

pick up all of task suite 050 (mark all [ ] with an hourglass), then start expansion at the beginning of the parent task and continue autonomously until done with this task suite, re-reading the instructions at the top of the FINAL_PASS_TASK_REMEDIATION, re-reading the PROMPT_TO_EXPAND_TASK, and re-reading CLAUDE.md first between each expansion to be sure it's all done right wiht a fresh context of the requirements. 

it's critically important to remember that presence of a section, or linecount, is not an indication of completion. this is because we could have a description with 300 lines of lorem ipsum, but that doesn't mean we skip and move on; that is semantically useless, so should be caught and replaced with adequate relevant semantically complete text according to our standards.

there is no such thing as a non-critical requirement here or i wouldn't be making these requirements. you don't get to judge what is important to me here.

 there are 16 sections that all need to be verified for EVERY SINGLE FILE in the suite. create a todo list to verify all 16 sections for all tasks in the suite (ie for suite 049 which has the parent and a,b,c,d,e,&f: 049 - header, 049 - description ... 049f - implementation prompt). you don't want to read and audit everything then plan to expand later, this would waste all the fresh context. The correct approach is:

Pick ONE task
Re-read it completely
Check each section - if incomplete, STOP and expand it immediately while context is fresh
Move to next section only after current is complete
When all 16 sections done, move to next task claimed by you, and if there are no more tasks claimed by you, then find the next unclaimed task suite and claim it and recursively begin expansion again. 
the only way to do that will be to actually read task to gain an understanding of it as a whole, and then read the section individually to semantically verify it actually captures the whole essence of the task in question, while maintaining AT A MINIMIUM the quality standards we've outlined. feel free to ultrathink if you can do so to have better results. 

 again to summarize: 
expand all sections required for all claimed tasks, one section at a time within one task at a time, so the whole task can be fresh and unconfused in the context... don't do all sections simultaneously for all tasks, or you're asking to overwhelm yourself and your context window by trying to focus on all tasks at once. get each task complete, one at a time, before moving to the next, and re-read the whole task before you begin expanding it to have a thorough view of the context of that task. also "Read your spellbook" (DND mage analogy -- read CLAUDE.md and PROMPT_TO_EXPAND_TASK before beginning work so you have a fresh eye on what you must do, the same way mages must read their spellbook EACH AND EVERY day they intend to cast spells, or else they will not be able to do so effectively) so read your spellbook before every day (expansion). 
that's why i told you to add a todo list for every item in every task, so for example for task 049, we had 112 items in the todo list, and you were to go through and verify for each if the section was complete. if it wasn't you stopped verifying and completed it, while you had fresh context. 