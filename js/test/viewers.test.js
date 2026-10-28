import * as spo from "../viewer.spo.js";
import * as jni from "../viewer.jni.js";
import * as txt from "../viewer.txt.js";
import * as ini from "../viewer.ini.js";

// Import and run in a browser served from the repository root.
export async function run() {
	let checks = 0;
	const assert = (condition, message) => {
		if (!condition) throw new Error(message);
		checks++;
	};
	const frame = () => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
	const target = document.createElement("article");
	target.style.width = "800px";
	document.body.append(target);
	const render = async (viewer, source, extension, mode = "file") => {
		target.className = mode === "inline" ? "wiki-embedded-inline" : mode === "embedded" ? "wiki-embedded-block" : "";
		viewer.render(source, {
			articleElement: target,
			filePath: "example." + extension,
			showSourceFilename: mode === "file",
			prepareArticle(className) { target.className = className; target.replaceChildren(); },
			createSourceFileUrl(path) { return "?file=" + path; }
		});
		await frame();
	};
	const scrollTo = async index => {
		const scroll = target.querySelector(".source-code, .source-embed");
		const lines = target.querySelectorAll(".source-lines > .source-line");
		scroll.scrollTop = lines[index].offsetTop + 2;
		scroll.dispatchEvent(new Event("scroll"));
		await frame();
		const sticky = target.querySelector(".source-sticky");
		assert(!sticky.hidden, "Sticky lines are visible after scrolling");
		assert(Math.abs(sticky.getBoundingClientRect().top - scroll.getBoundingClientRect().top) < 3, "Sticky lines stay at the scroll viewport's top");
		return Array.from(sticky.children, line => Number(line.dataset.line));
	};
	try {
		assert(!spo.extensions.includes("jni") && jni.extensions.includes("jni"), "JNI uses its own viewer");
		for (const [viewer, extension, source] of [
			[txt, "txt", "<script>not code</script>\n\tsecond\n"],
			[spo, "spo", 'root (\n\tname = "text"\n)\n'],
			[spo, "ilt", 'IF true\n\tFROM source\n'],
			[jni, "jni", "root:\n|key=value\n|body:\n||<script>raw</script>\n"],
			[ini, "ini", "; comment\n[section]\nkey=<script>raw</script>\n"]
		]) {
			for (const mode of ["file", "embedded", "inline"]) {
				await render(viewer, source, extension, mode);
				assert(target.querySelectorAll("script").length === 0, extension + ": source text stays escaped");
				if (mode === "inline") {
					assert(target.textContent === (extension === "jni" ? source.replace(/\n/g, "") : source), extension + ": inline text preserved");
					assert(!target.querySelector(".source-gutter"), extension + ": inline has no gutter");
				} else {
					const rows = Array.from(target.querySelector(".source-lines").children);
					assert(JSON.stringify(rows.map(row => row.querySelector(".source-line-text").textContent)) === JSON.stringify(source.split("\n")), extension + ": rows preserved");
					assert(rows.every((row, index) => row.dataset.line === String(index + 1)), extension + ": shared line numbers");
					assert(rows.at(-1).offsetHeight > 0, extension + ": empty last row retains its height");
				}
			}
		}
		const source = ["root:", "|block:", "||plain text", "|||deeper text", ...Array.from({ length: 90 }, (_, i) => "||||leaf " + i), "|sibling:", ...Array.from({ length: 40 }, (_, i) => "||other " + i)].join("\n");
		for (const mode of ["file", "embedded"]) {
			await render(jni, source, "jni", mode);
			assert(target.querySelector('.source-lines > [data-line="4"] .jni-plain'), "JNI literal text stays plain while contributing its pipe depth");
			assert(JSON.stringify(await scrollTo(20)) === "[1,2,3,4]", "JNI counts every leading pipe in plain values");
			assert(JSON.stringify(await scrollTo(105)) === "[1,95]", "JNI drops old ancestors at a sibling block");
		}
		await render(jni, [...Array.from({ length: 12 }, (_, i) => "|".repeat(i) + "text " + i), ...Array.from({ length: 70 }, (_, i) => "|".repeat(12) + "leaf " + i)].join("\n"), "jni");
		assert(JSON.stringify(await scrollTo(35)) === "[7,8,9,10,11,12]", "Sticky hierarchy retains its six innermost lines");
		for (const viewer of [spo, txt]) {
			await render(viewer, ["root", "\tchild", ...Array.from({ length: 70 }, (_, i) => "\t\tleaf " + i)].join("\n"), viewer === spo ? "spo" : "txt");
			assert(JSON.stringify(await scrollTo(20)) === "[1,2]", "SPO and TXT share whitespace indentation hierarchy");
		}
		await render(ini, ["[first]", ...Array.from({ length: 65 }, (_, i) => "key" + i + "=value"), "[second]", ...Array.from({ length: 40 }, (_, i) => "key" + i + "=value")].join("\n"), "ini");
		assert(JSON.stringify(await scrollTo(20)) === "[1]", "INI section remains sticky");
		assert(JSON.stringify(await scrollTo(80)) === "[67]", "INI switches its sticky section");
		target.style.width = "650px";
		await frame();
		const scroll = target.querySelector(".source-code");
		assert(scroll.scrollWidth <= scroll.clientWidth + 1, "Sticky widths shrink without causing horizontal overflow");
		target.style.width = "1000px";
		await frame();
		assert(target.querySelector(".source-sticky").offsetWidth >= scroll.clientWidth, "Sticky widths follow the resized viewport");
		assert(document.querySelectorAll("#source-viewer-style").length === 1, "Shared styles are installed once");
		return { checks };
	} finally {
		target.remove();
		await frame();
	}
}
