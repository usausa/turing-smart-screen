---
paths:
  - "**/*.cs"
---

### Member Variables

- Do not prefix member variables with `_`.

### Parentheses in Logical Expressions (`&&` / `||`)

Parenthesize each operand of `&&` / `||` when it is a comparison (`==`, `!=`, `<`, `<=`, `>`, `>=`) or an `is` expression. Analyzers do not enforce this, so it must be written by hand.

- `if ((count == 1) && flag)` instead of `if (count == 1 && flag)`
- `if ((key is not null) && flag)` instead of `if (key is not null && flag)`
- `obj is Foo other && Equals(other)` becomes `(obj is Foo other) && Equals(other)`
- Operands that are already a single identifier, member access, or method call need no parentheses: `if (flag && IsReady())`
