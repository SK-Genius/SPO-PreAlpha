[
	§SIG_WITH F € [§TYPE => [§LOAD "../Types/TypeConstructor.SIG"]] IN [<
		OK...: [
			§GENERIC tValue [
				§GENERIC tError [
					[tError, tValue] => [.(.F tValue) tError]
				]
			]
		]
		Fail...: [
			§GENERIC tValue [
				§GENERIC tError [
					[tError, tValue] => [.(.F tValue) tError]
				]
			]
		]
		...>>...: [
			§GENERIC tIn [
				§GENERIC tOut [
					§GENERIC tError [
						[
							[.(.F tIn) tError]
							tIn => tOut
						] => [.(.F tOut) tError]
					]
				]
			]
		]
		...>>?...: [
			§GENERIC tIn [
				§GENERIC tOut [
					§GENERIC tError [
						[
							[.(.F tIn) tError]
							tIn => [.(.F tOut) tError]
						] => [.(.F tOut) tError]
					]
				]
			]
		]
		...Or...: [
			§GENERIC tValue [
				§GENERIC tError [
					[
						[.(.F tValue) tError]
						tValue
					] => tValue
				]
			]
		]
		...OrDo...: [
			§GENERIC tValue [
				§GENERIC tError [
					[
						[.(.F tValue) tError]
						tError => tValue
					] => tValue
				]
			]
		]
		...OrTry...: [
			§GENERIC tValue [
				§GENERIC tError [
					[
						[.(.F tValue) tError]
						tError => [.(.F tValue) tError]
					] => [.(.F tValue) tError]
				]
			]
		]
		...MapError...: [
			§GENERIC tValue [
				§GENERIC tErrorIn [
					§GENERIC tErrorOut [
						[
							[.(.F tValue) tErrorIn]
							tErrorIn => tErrorOut
						] => [.(.F tValue) tErrorOut]
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
					] => [.(.F tValue) tError]
				]
			]
		]
	>]
]
