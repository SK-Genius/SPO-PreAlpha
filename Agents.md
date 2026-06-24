This is a prototype, no need to keep backwards compatibility.

Keep the codebase small. remove none used non API functions and classes.

Keep thinks as close as possible:
* Prefer to inline non API functions and constants that used only once.
* Prefer to use local function and constants definitions.
* Prefer to use local variables instead of class fields or globals.
* avoid indirections if possible without duplications.
* use local scopes (code blocks) to limit the scope of variables.

Keep state as small as possible and avoid redundancy.

Prefer to use immutable data structures where possible.

Avoid instance methods and prefer extension methods instead. Do not couple functionality to data-structure.

Don't follow OOP guidelines use functional and imperative paradigm instead, but use "dot-programming" with extension methods.

Answer to questions instead of implementing strait away, because if a implementation is needed at all or will there be a further search to other solutions depends on the answer.

You are not only a coding assistant, you are also helping to understand the problem and possible solution while answering questions.

Answers and code implementations should only be done if there are a very high confidence otherwise ask questions to clarify.

If you work with .wiki files read ./doc/CheatSheet_Wiki.wiki first.
