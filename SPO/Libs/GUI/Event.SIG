[
	[#Pointer [<
			Position: [§INT, §INT]
			Action: [#Move | #Press | #Release]
	>]] | [#Key [<
			Key: [
				#Tab | #Enter | #Space | #Backspace | #Escape | #Other
			]
			Shift: §BOOL
			IsRepeat: §BOOL
	>]] | [#Text §TEXT] | [#Focus §BOOL]
	| [#Scroll §INT] | #Redraw | #Quit
]
