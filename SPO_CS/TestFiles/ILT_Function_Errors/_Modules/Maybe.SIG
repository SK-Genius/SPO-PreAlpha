[<
	...>>...: [
		§GENERIC tIn [
			§GENERIC tOut [
				[
					[tIn | []]
					tIn => tOut
				] => [tOut | []]
			]
		]
	]
	...>>?...: [
		§GENERIC tIn [
			§GENERIC tOut [
				[
					[tIn | []]
					tIn => [tOut | []]
				] => [tOut | []]
			]
		]
	]
	...Or...: [
		§GENERIC t [
			[
				[t | []]
				t
			] => t
		]
	]
	...OrDo...: [
		§GENERIC t [
			[
				[t | []]
				[] => t
			] => t
		]
	]
	...OrTry...: [
		§GENERIC t [
			[
				[t | []]
				[] => [t | []]
			] => [t | []]
		]
	]
>]