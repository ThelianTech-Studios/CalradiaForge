# Task Execution Optimization Policy

## Purpose

This policy defines how Codex workflows should optimize task execution while
preserving correctness, maintainability, architectural consistency, and
verification quality. It centralizes guidance for model capability, reasoning
effort, parallelization, and task decomposition so the same principles can be
reused across planning, implementation, verification, and reviewer workflows.

## Scope

Apply this policy whenever a Codex workflow evaluates how work should be
assigned, decomposed, reasoned about, parallelized, or reviewed. It applies to
the main agent and to any subagents used for implementation, documentation,
validation, auditing, or review.

This policy governs execution choices. It does not replace project
architecture, safety, scope, or implementation requirements.

## Optimization Dimensions

Before delegating work, evaluate the assigned task across four independent
optimization dimensions:

- Capability Selection (model capability)
- Reasoning Effort
- Parallelization
- Scope

Treat each optimization dimension as an independent decision. Optimize each
according to the assigned work rather than applying the same settings to every
task or subagent.

### Capability Selection

- Prefer the lowest-capability model capable of reliably completing the assigned task.
- When the runtime supports per-subagent model selection, apply capability- and cost-aware task triage before delegation.
- Keep routine, deterministic, localized, documentation, validation, read-heavy, and mechanical implementation work on the lowest-capability suitable model (for example, Luna).
- Escalate individual subagents to higher-capability models available in the current runtime (for example, Terra or Sol) only when their assigned work materially benefits from stronger reasoning, including:
  - Cross-cutting architectural analysis.
  - Ambiguous implementation decisions.
  - Complex debugging or root-cause analysis.
  - Security-sensitive reviews.
  - Concurrency or threading analysis.
  - Large multi-file reasoning.
  - Reviewer findings requiring significant engineering judgment.
- Do not escalate simply because a higher-capability model is available.

### Reasoning Effort

- When the runtime supports configurable reasoning effort, choose the lowest reasoning level capable of reliably completing the assigned task.
- Increase reasoning effort only when additional analysis is expected to materially improve correctness, confidence, or engineering quality.
- Use higher reasoning effort for tasks involving architecture, complex debugging, reviewer analysis, cross-component interactions, concurrency, security, performance analysis, or other high-risk engineering work.
- Do not automatically increase reasoning effort when using a higher-capability model. Treat model capability and reasoning effort as independent optimization decisions.

### Parallelization

- Only create subagents when doing so is expected to improve overall efficiency, reduce implementation risk, or allow safe parallel execution.
- Avoid unnecessary subagent creation for small, tightly coupled, or sequential work where orchestration overhead outweighs the benefit.
- When using subagents, keep assignments independent whenever practical to minimize merge conflicts and unnecessary coordination.

### Scope

- Assign each subagent a clear, well-defined, non-overlapping responsibility.
- Prefer small, cohesive scopes over broad implementation assignments.
- Avoid assigning multiple subagents responsibility for the same files, components, or architectural concern unless explicitly required.
- Keep each assignment focused enough that the assigned agent can reason about the entire scope without unnecessary context switching.

## General Policy

- Treat model capability, reasoning effort, parallelization, and task scope as optimization mechanisms rather than functional requirements.
- The workflow must remain correct even if runtime limitations prevent model selection, reasoning adjustment, or subagent creation.
- Favor correctness, maintainability, architectural consistency, and verification quality over maximizing model capability or reasoning effort.

## Guiding Principles

- Optimize for the assigned work, not for uniformity across tasks.
- Keep delegation proportional to the size, risk, and independence of the work.
- Preserve clear ownership and minimize coordination overhead.
- Use additional capability or reasoning only when it materially improves the expected result.
- Treat verification quality and behavior preservation as primary outcomes of optimization.

## Runtime Compatibility

Runtime capabilities may differ. If the current runtime does not support
per-subagent model selection, configurable reasoning effort, or subagent
creation, continue the workflow with the closest supported behavior. Do not
skip required planning, implementation, verification, or reviewer steps merely
because an optimization mechanism is unavailable.

