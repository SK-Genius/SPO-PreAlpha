[
	§RECURSIVE tElement [
		[#Text [<
				Text: §TEXT
				Color: [§INT, §INT, §INT]
		>]] | [#Button [<
				Id: §INT
				Text: §TEXT
				Selected: §BOOL
		>]] | [#TextInput [<
				Id: §INT
				Label: §TEXT
				Value: §TEXT
		>]] | [#Paragraph [<
				Text: §TEXT
				Alignment: [§LOAD "Alignment.SIG"]
				Color: [§INT, §INT, §INT]
		>]] | [#List [<
				Axis: [#Horizontal | #Vertical]
				Alignment: [§LOAD "Alignment.SIG"]
				CrossAlignment: [§LOAD "Alignment.SIG"]
				Padding: §INT
				Spacing: §INT
				Color: [§INT, §INT, §INT]
				Children: [.([§LOAD "../ListType.SIG"]) tElement]
		>]]
	]
]
