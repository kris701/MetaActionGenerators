# Meta Action Generators
This is a project that is a collection of several different meta action generators.
Some of the generators have some dependencies they need to work, however they are all described in their respective section.

## Generators
Here is a short description of all the different generators that this project includes.

### Stripped Meta Actions
This is simply meta actions that consists of taking all actions in the original domain and removing their preconditions.

### Predicate Meta Actions
These are meta actions that consists of a single predicate in the effects, with no preconditions.
There is a "true" and a "false" version of all the predicates.

### Flip Meta Actions
These are meta actions that flip the value of a single predicate, i.e. the precondition requires a predicate to be true, and the effect sets it to false.

