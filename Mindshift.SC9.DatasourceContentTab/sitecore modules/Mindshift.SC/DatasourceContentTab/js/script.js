window.onload = function () {
	var router = new VueRouter({
		mode: 'history',
		routes: []
	})

	var app = new Vue({
		router,
		el: '#app',
		data: {
			dynamicResponse: null,
			view: {
				currentLayoutType: { Name: '' }
			}
		},
		created: function () {
			this.getRenderings()
		},        
		methods: {
			getRenderings: function () {

				//let query = 'id=' + this.$route.query.id +
				//	'&database=' + this.$route.query.database +
				//	'&version=' + this.$route.query.version +
				//	'&language=' + this.$route.query.language

				// note: we pass the querystring right along to the API
				this.$http.get('/mindshiftAPI/DatasourceContentTab/GetRenderings' + window.location.search)
					.then(response => this.dynamicResponse = response.data)
			},
			selectLayoutType: function (layoutType) {
				//layoutType.selected = true;
				//for (let i = 0; i < dynamicResponse.layoutTypes; i++) {
				//	dynamicResponse[i].selected = false;
				//}
				this.view.currentLayoutType = layoutType
			},
			setOpenState: function (rendering) {
				//var id = rendering.UniqueId ? rendering.UniqueId : rendering.Id;
				//if (!$scope.state.openState[id]) $scope.state.openState[id] = false;
				//view.state.openState[id] = !$scope.state.openState[id];
				//saveState();
			},
			getOpenState: function (rendering) {
				return true
				//var id = rendering.UniqueId ? rendering.UniqueId : rendering.Id;
				//if (!$scope.state.openState) $scope.state.openState = {}; // shouldn't ever be needed - just needed it because I added it later
				//return view.state.openState[id]; // note: null or undefined is false so...
			},
			getEditState: function (rendering) { return true }
		}

	})
}

///mindshiftAPI/DatasourceContentTab/GetRenderings?id=%7B9DFBB778-512F-4BA7-8293-C01503572D30%7D&language=en&version=1&database=master