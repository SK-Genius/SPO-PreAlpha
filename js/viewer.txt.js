import { renderSource } from "./source-view.js";

export const extensions = ["txt", "text"];
export const isBinary = false;

export function render(source, context) {
	renderSource(source, context, { language: "TXT" });
}

export default Object.freeze({ extensions, isBinary, renderer: { render } });
