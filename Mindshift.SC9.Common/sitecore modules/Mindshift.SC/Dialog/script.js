window.onload = function () {
	let waitFor = function (condition, callback) {
		if (!condition()) {
			window.setTimeout(waitFor.bind(null, condition, callback), 100); /* this checks the flag every 100 milliseconds*/
		} else {
			callback();
		}
	}

	var router = new VueRouter({
		mode: 'history',
		routes: []
	})

	window.router = router;
	// TODO: dialog querystring is path to js file!



	options = {
		namespace: 'mindshift_sc__', // key prefix
		name: 'ls', // name variable Vue.[ls] or this.[$ls],
		storage: 'local', // storage name session, local, memory
	};

	Vue.use(VueStorage, options)

	var app = new Vue({
		router,
		el: '#app',
		data: {
			currentRoute: window.location.pathname
		},
		props: {
			componentName: {
				type: String,
				default: () => null
			}
		},
		created: function () {
			let componentName = this.$route.query.dialog;

			let dialogScript = document.createElement('script')
			let dialogScriptUrl = '/sitecore modules/Mindshift.SC/Dialog/' + componentName + '.js'
			dialogScript.setAttribute('src', dialogScriptUrl)
			document.head.appendChild(dialogScript)

			let dialogCss = document.createElement('link')
			let dialogCssUrl = '/sitecore modules/Mindshift.SC/Dialog/' + componentName + '.css'
			dialogCss.setAttribute('rel', 'stylesheet')
			dialogCss.setAttribute('href', dialogCssUrl)
			document.head.appendChild(dialogCss)

			// we're waiting for the dyamically loaded script to actually create the component
			waitFor(() => componentName in Vue.options.components, () => this.componentName = componentName)

		},
		methods: {

		},
		computed: {
			currentComponent: function () {
				return this.componentName
			}

		}

	})
}

///mindshiftAPI/DatasourceContentTab/GetRenderings?id=%7B9DFBB778-512F-4BA7-8293-C01503572D30%7D&language=en&version=1&database=master