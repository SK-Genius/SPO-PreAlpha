import * as cs from "../viewer.cs.js";
import * as txt from "../viewer.txt.js";

// Import and run in a browser served from the repository root.
export async function run() {
	let checks = 0;
	const assert = (condition, message) => {
		if (!condition) throw new Error(message);
		checks++;
	};
	const target = document.createElement("article");
	document.body.append(target);
	const render = (source, inline = true) => {
		target.className = inline ? "wiki-embedded-inline" : "wiki-embedded-block";
		cs.render(source, {
			articleElement: target,
			filePath: "example.cs",
			showSourceFilename: false,
			prepareArticle(className) { target.className = className; target.replaceChildren(); }
		});
		return Array.from(target.querySelectorAll(inline ? ".source-inline > span" : ".source-lines .source-line-text > span"));
	};
	const frame = () => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
	const stickyLines = async (source, lineIndex, viewer = cs) => {
		target.className = "wiki-embedded-block";
		viewer.render(source, {
			articleElement: target,
			filePath: viewer === cs ? "example.cs" : "example.txt",
			showSourceFilename: false,
			prepareArticle(className) { target.className = className; target.replaceChildren(); }
		});
		const scroll = target.querySelector(".source-embed");
		scroll.style.height = "350px";
		await frame();
		scroll.scrollTop = target.querySelectorAll(".source-lines > .source-line")[lineIndex].offsetTop + 2;
		scroll.dispatchEvent(new Event("scroll"));
		await frame();
		return Array.from(target.querySelector(".source-sticky").children, line => Number(line.dataset.line));
	};
	try {
		const source = "tMyType t iProcDef aMyArgs a a1 _1 cMyConst mMyModule gMyState _MyField " +
			"@aName @class title array caller Result public int string extension nameof(aValue)";
		const tokens = render(source);
		for (const [role, names] of [
			["cs-type", ["tMyType", "t", "iProcDef", "int", "string"]],
			["cs-argument", ["aMyArgs", "a", "a1", "_1", "@aName", "aValue"]],
			["cs-constant", ["cMyConst"]],
			["cs-module", ["mMyModule"]],
			["cs-global", ["gMyState"]],
			["cs-field", ["_MyField"]],
			["identifier", ["@class", "title", "array", "caller", "Result"]],
			["keyword", ["public", "extension", "nameof"]]
		]) {
			for (const name of names) {
				assert(tokens.some(token => token.textContent === name && token.classList.contains("tok-" + role)), name + ": " + role);
			}
		}
		assert(target.textContent === source, "Prefix highlighting preserves the source");
		for (const literal of [
			String.raw`"tMyType aValue cConst \\\" <script>"`,
			String.raw`@"cConst ""aValue"" \ tType"`,
			'@"first\r\naValue\ncConst\rfinal"',
			'"""\n\tvar aValue = "<img>";\n\t"""',
			'""""embedded """ quotes""""',
			String.raw`$"{aValue.ToString("N2")} {{cConst}}"`,
			String.raw`$@"{aValue.ToString("N2")} ""quotes"""`,
			String.raw`@$"{aValue.ToString("N2")}"`,
			'$$"""literal {cConst} {{aValue.ToString("N2")}}"""',
			'$"{new { Value = "aValue" }.Value}"',
			'$"{\n aValue switch { 1 => "yes", _ => "no" }\n}"',
			String.raw`'\''`,
			String.raw`'\\'`,
			'"aValue"u8',
			'"""aValue"""u8'
		]) {
			const text = "var Text = " + literal + "; tAfter aAfter cAfter;";
			const values = render(text);
			assert(values.filter(token => token.classList.contains("tok-string")).map(token => token.textContent).join("") === literal, "Complete literal: " + literal);
			assert(values.some(token => token.textContent === "aAfter" && token.classList.contains("tok-cs-argument")), "Code after the literal is highlighted");
			assert(target.textContent === text && !target.querySelector("script,img"), "Literal source stays escaped");
		}
		for (const comment of ["// tType aValue cConst <script>", "/* tType\naValue\ncConst */", "/* unfinished aValue"]) {
			const values = render(comment);
			assert(values.every(token => token.classList.contains("tok-comment")), "Names inside comments have no prefix colors");
			assert(target.textContent === comment, "Comment text is preserved");
		}
		const adjacent = render("Value=/* aValue */ONE;// tType");
		assert(adjacent.filter(token => token.classList.contains("tok-comment")).length === 2, "Comments are recognized directly after operators");
		const directive = render('#:property Name = "aValue"\n#if DEBUG\naValue\n#endif');
		assert(directive.filter(token => token.classList.contains("tok-cs-directive")).length === 3, "File-based and conditional directives are recognized");
		const numbers = ["0x_FFUL", "0b_1010", "1_000", "1.5e-3F", ".5m", "42u", "42L"];
		assert(render(numbers.join(" ")).filter(token => token.classList.contains("tok-number")).map(token => token.textContent).join(" ") === numbers.join(" "), "C# numeric forms and suffixes are recognized");
		const text = '/* first\r\nlast */\r\nvar Text = """\r\naValue\r\n""";\r\naAfter\r\n';
		render(text, false);
		const lines = Array.from(target.querySelectorAll(".source-lines .source-line-text"));
		assert(lines.map(line => line.textContent).join("\n") === text.replace(/\r\n/g, "\n"), "Multiline tokens keep their rows and final empty line");
		assert(lines[1].querySelector(".tok-comment") && lines[4].querySelector(".tok-string") && lines[5].querySelector(".tok-cs-argument"), "Token state survives line boundaries and ends correctly");
		assert(document.querySelectorAll("#cs-viewer-style").length === 1, "C# styles are installed once");
		const body = Array.from({ length: 80 }, (_, index) => "\t\t\taValue = " + index + ";");
		const signature = [
			"\tpublic static tBool", "\tIsInterface(", "\t\tthis tType aType,",
			"\t\t[MaybeNullWhen(false)] out tType aHeadType,", "\t\t[MaybeNullWhen(false)] out tType aBodyType",
			"\t\t// aCond", "\t) {", "\t\t// bla", "\t\tif (aType.Kind is tKind.Interface) {",
			"\t\t\taHeadType = aType.Refs[0];", "\t\t\taBodyType = aType.Refs[1];", "\t\t\treturn true;",
			"\t\t} else {", "\t\t\taHeadType = default!;", "\t\t\taBodyType = default!;", ...body, "\t\t}", "\t}"
		].join("\n");
		assert(JSON.stringify(await stickyLines(signature, 14)) === "[2,7,9,13]", "Method name and if stay pinned before their closers; the return-type line stays hidden");
		const chain = ["Start(", ")", "]", "}", ">", ")", "]", "}", "> {", ...body].join("\n");
		assert(JSON.stringify(await stickyLines(chain, 12)) === "[1,2,3,4,5,6,7,8,9]", "A closer chain keeps every preceding sibling through the first non-closer, even beyond six lines");
		const siblings = ["Old(", "\targument", ")", "New(", "\targument", ") {", ...body].join("\n");
		assert(JSON.stringify(await stickyLines(siblings, 10)) === "[4,6]", "Lookback stops at the first non-closing sibling");
		const blanks = ["Start(", ")", "", "]", "> {", ...body].join("\n");
		assert(JSON.stringify(await stickyLines(blanks, 10)) === "[1,2,4,5]", "Blank lines are skipped during same-level lookback");
		const scopes = ["Old {", "\tFirst(", "\t) {", "\t\targument", "}", "New {", "\t) {", ...body].join("\n");
		assert(JSON.stringify(await stickyLines(scopes, 10)) === "[6,7]", "Lookback does not cross into a previous enclosing scope");
		for (const opening of ['var Text = """', "/*"]) {
			assert(JSON.stringify(await stickyLines([opening, ")", ...body].join("\n"), 6)) === "[2]", "Closing characters inside literals and comments do not attach code context");
		}
		assert(JSON.stringify(await stickyLines(["Start(", ") {", ...body].join("\n"), 6, txt)) === "[2]", "Other source viewers keep their existing sticky behavior");
		return { checks };
	} finally {
		target.remove();
	}
}
