[
	§SIG_WITH tResult € [§TYPE => [§TYPE => §TYPE]] IN [<
		OK...: [
			§GENERIC tValue [
				§GENERIC tError [
					[tError, tValue] => [.(.tResult tValue) tError]
				]
			]
		]
		Fail...: [
			§GENERIC tValue [
				§GENERIC tError [
					[tError, tValue] => [.(.tResult tValue) tError]
				]
			]
		]
		...>>...: [
			§GENERIC tIn [
				§GENERIC tOut [
					§GENERIC tError [
						[
							[.(.tResult tIn) tError]
							tIn => tOut
						] => [.(.tResult tOut) tError]
					]
				]
			]
		]
		...>>?...: [
			§GENERIC tIn [
				§GENERIC tOut [
					§GENERIC tError [
						[
							[.(.tResult tIn) tError]
							tIn => [.(.tResult tOut) tError]
						] => [.(.tResult tOut) tError]
					]
				]
			]
		]
		...Or...: [
			§GENERIC tValue [
				§GENERIC tError [
					[
						[.(.tResult tValue) tError]
						tValue
					] => tValue
				]
			]
		]
		...OrDo...: [
			§GENERIC tValue [
				§GENERIC tError [
					[
						[.(.tResult tValue) tError]
						tError => tValue
					] => tValue
				]
			]
		]
		...OrTry...: [
			§GENERIC tValue [
				§GENERIC tError [
					[
						[.(.tResult tValue) tError]
						tError => [.(.tResult tValue) tError]
					] => [.(.tResult tValue) tError]
				]
			]
		]
		...MapError...: [
			§GENERIC tValue [
				§GENERIC tErrorIn [
					§GENERIC tErrorOut [
						[
							[.(.tResult tValue) tErrorIn]
							tErrorIn => tErrorOut
						] => [.(.tResult tValue) tErrorOut]
					]
				]
			]
		]
		...OrFail...: [
			§GENERIC tValue [
				§GENERIC tError [
					[
						[tValue | []]
						[] => tError
					] => [.(.tResult tValue) tError]
				]
			]
		]
	>]
]