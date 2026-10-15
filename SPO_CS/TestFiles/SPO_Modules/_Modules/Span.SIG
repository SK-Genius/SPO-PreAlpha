[
	§SIG_WITH tSpan € [§TYPE => §TYPE] IN [<
		...--...: [§GENERIC t [[t, t] => [.tSpan t]]]
		SpanAt...: [§GENERIC t [t => [.tSpan t]]]
		...Start: [§GENERIC t [[.tSpan t] => t]]
		...End: [§GENERIC t [[.tSpan t] => t]]
		...&...With...: [
			§GENERIC t [[.tSpan t, .tSpan t, [t, t] => §BOOL] => [.tSpan t]]
		]
		...==...With...: [
			§GENERIC t [
				[
					.tSpan t
					.tSpan t
					[t, t] => §BOOL
				] => §BOOL
			]
		]
		==With...: [
			§GENERIC t [
				[[t, t] => §BOOL] => [[.tSpan t, .tSpan t] => §BOOL]
			]
		]
	>]
]