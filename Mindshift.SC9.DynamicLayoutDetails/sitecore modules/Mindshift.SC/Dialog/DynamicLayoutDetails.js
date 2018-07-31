
// TODO: they shouldn't be plural!
// TODO: separate placeholders from renderings!

// TODO: pass the rendering to this!
let placeholderComponent = Vue.extend({
	router,
	name: 'DynamicLayoutDetailsPlaceholder',
	props: ['placeholder', 'device', 'rendering'],
	data: function () {
		return {
			dynamicResponse: null,
			view: {
				currentLayoutType: { Name: '' }
			},
			state: {
				openState: false
			},
		}
	},
	template: `
	<li>
		<div :class="['title', 'placeholder-title', placeholder.Exists ? 'valid': 'invalid']">
			<div class="arrow" @click="state.openState=!state.openState" v-if="placeholder.Renderings.length > 0"><i :class="['fas', state.openState ? 'fa-angle-down': 'fa-angle-right']"></i></div>
			<div class="new-position" v-if="view.newPositionRendering && rendering && view.newPositionRendering.UniqueId != rendering.UniqueId">
				<i class="fas fa-angle-right"></i>
			</div>
			<span v-html="placeholder.Icon"></span>
			{{placeholder.Name}} <span title="children">({{placeholder.Renderings.length}})</span>
			<span class="badge badge-pill badge-info" v-if="placeholder.Dynamic" title="Dynamic"><i class="fas fa-bolt fa-xs"></i></span>
			
			<!--<button @click="addRendering(placeholder)" title="add rendering"><span class="fas fa-plus"></span></button>-->
			<button @click="setAddRenderingState(device, rendering, placeholder)" title="add child rendering" v-if="placeholder.Exists" style="display: none"><i class="fas fa-plus"></i></button>
			<div v-if="placeholder.DynamicRecommended">
				Warning: This Placeholder is not Dynamic, but there are multiple of it on the page so you should change it to be Dynamic.
			</div>		
		
			
			


			<div class="add-rendering-panel" v-if="getAddRenderingState(device, rendering, placeholder)">
				<div class="title">Add Rendering</div>
				<ul>
					<li v-for="placeholderRendering in placeholder.ValidRenderings" @click="addRendering(device, placeholder, rendering, placeholderRendering)"><span v-html="placeholderRendering.Icon"></span> {{placeholderRendering.DisplayName}}</li>
				</ul>
			</div>
		</div>
		<ul v-if="state.openState">
			<DynamicLayoutDetailsRendering v-for="rendering in placeholder.Renderings" :key="rendering.UniqueId" :placeholder="placeholder" :device="device" :rendering="rendering"></DynamicLayoutDetailsRendering>
		</ul>
	</li>
	`,
	computed: {
		// TODO: this fails, fix it later
		id: function () { return this.placeholder ? this.placeholder.Name + '-' + this.placeholder.ParentUniqueId : false }
	},
	created: function () {
		let stateCookieString = this.$ls.get('dynamiclayoutdetails-stateCookie');
		let stateCookie = JSON.parse(stateCookieString)
		if (stateCookie) {
			this.state.openState = stateCookie.openState[this.id] ? stateCookie.openState[this.id] : false
		}
	},
	methods: {
		getAddRenderingState: function (device, obj, placeholder) {
			let id = device.Name
			if (obj) id += '-' + obj.UniqueId ? obj.UniqueId : obj.Id
			id += '~' + placeholder.Name
			return false; //this.state.addRenderingState[id]; // TODO: uncomment this when we go vuex

		}
	},
	watch: {
		'state.openState': function (newVal, oldVal) {
			let stateCookieString = this.$ls.get('dynamiclayoutdetails-stateCookie');
			let stateCookie = JSON.parse(stateCookieString)
			if (stateCookie) {
				stateCookie.openState[this.id] = this.state.openState
				this.$ls.set('dynamiclayoutdetails-stateCookie', JSON.stringify(stateCookie))
			}
		}
	}
})

Vue.component('DynamicLayoutDetailsPlaceholder', placeholderComponent)


let renderingComponent = Vue.extend({
	router,
	name: 'DynamicLayoutDetailsRendering',
	props: ['placeholder', 'device', 'rendering'],
	data: function () {
		return {
			dynamicResponse: null,
			view: {
				currentLayoutType: { Name: '' }
			},
			state: {
				openState: false,
				editState: false
			},
		}
	},
	template: `
	<li :title="'uniqueId: ' + rendering.UniqueId + ', seed: ' + rendering.PlaceholderSeed">
		<div class="title">
			<button v-if="view.newPositionRendering && view.newPositionRendering.UniqueId == rendering.UniqueId" @click="moveItemOrder(rendering, placeholder, 1)" title="move rendering up"><i class="fas fa-chevron-double-up"></i></button>
			<button v-if="view.newPositionRendering && view.newPositionRendering.UniqueId == rendering.UniqueId" @click="moveItemOrder(rendering, placeholder, -1)" title="move rendering down"><i class="fas fa-chevron-double-down"></i></button>
			<div class="arrow" @click="state.openState=!state.openState" v-if="rendering.Placeholders.length > 0"><i :class="['fas', state.openState ? 'fa-angle-down': 'fa-angle-right']"></i></div>
			{{rendering.OrderBy}}
			<span v-html="rendering.Icon"></span>
			{{rendering.DisplayName}} ({{rendering.Placeholders.length}})
			<button @click="setMoveState(rendering, placeholder)" title="move rendering" style="display: none"><i :class="view.newPositionRendering && view.newPositionRendering.UniqueId == rendering.UniqueId ? 'fas fa-trash': 'fas fa-arrows'" style="display: none"></i></button>
			<button class="btn btn-outline-secondary btn-sm" @click="state.editState=!state.editState" title="edit rendering" :class="{ active: state.editState }"><i class="fa fa-edit"></i></button>
			<div class="edit-panel" v-if="state.editState">
				<table>
					<tbody>
						<tr>
							<th>UniqueId:</th>
							<td>{{rendering.UniqueId}}</td>
						</tr>

						<tr :title="'DatasourceId: ' + rendering.DataSourceId">
							<th>Datasource:</th>
							<td><input type="text" v-model="rendering.DataSourcePath" /></td>
						</tr>
						<tr>
							<th>Placeholder Path:</th>
							<td><input type="text" v-model="rendering.PlaceholderPath" :class="['title', 'placeholder-title', placeholder.Exists ? 'valid': 'invalid']" /></td>
						</tr>

						<tr v-if="rendering.InvalidDynamicPlaceholder">
							<th>Warnings:</th>
							<td>
								This rendering was added to the last instance of a Dynamic Placeholder because it did not seem to be in the format for a Dynamic Placeholder. <br><br>
								Possible values (for the last part of the path) could be:
								<ul>
									<li v-for="possiblePlaceholderPath in rendering.PossiblePlaceholderPaths">{{possiblePlaceholderPath}}</li>
								</ul>

								InvalidDynamicPlaceholder

							</td>
						</tr>		
					</tbody>
				</table>
			</div>
		</div>
		<div class="list-body">
			<ul v-if="state.openState">
				<DynamicLayoutDetailsPlaceholder v-for="placeholder in rendering.Placeholders" :key="placeholder.Name" :placeholder="placeholder" :device="device" :rendering="rendering"></DynamicLayoutDetailsPlaceholder>
			</ul>
		</div>
	</li>

	`,
	computed: {
		// TODO: this fails, fix it later
		id: function () { return this.rendering ? (this.rendering.UniqueId ? this.rendering.UniqueId : this.rendering.Id) : false },
		placeholderTooltip: function () {
			let ret = ''
			if (!this.placeholder.Exists) {
				if (this.rendering.PossiblePlaceholderPaths.length > 0) {
					ret += '\n'
					for (let i = 0; i < this.rendering.PossiblePlaceholderPaths.length; i++) {
						ret += this.rendering.PossiblePlaceholderPaths[i] + '\n'
					}
				}
				
			}
			return ret
		}
	},
	created: function () {
		let stateCookieString = this.$ls.get('dynamiclayoutdetails-stateCookie');
		let stateCookie = JSON.parse(stateCookieString)
		if (stateCookie) {
			this.state.openState = stateCookie.openState[this.id] ? stateCookie.openState[this.id] : false
		}
	},
	methods: {
		getAddRenderingState: function (device, obj, placeholder) {
			let id = device.Name
			if (obj) id += '-' + obj.UniqueId ? obj.UniqueId : obj.Id
			id += '~' + placeholder.Name
			return false; //this.state.addRenderingState[id]; // TODO: uncomment this when we go vuex

		}
	},
	watch: {
		'state.openState': function (newVal, oldVal) {
			let stateCookieString = this.$ls.get('dynamiclayoutdetails-stateCookie');
			let stateCookie = JSON.parse(stateCookieString)
			if (stateCookie) {

				stateCookie.openState[this.id] = this.state.openState
				this.$ls.set('dynamiclayoutdetails-stateCookie', JSON.stringify(stateCookie))
			}
		}
	}
})

Vue.component('DynamicLayoutDetailsRendering', renderingComponent)

let deviceComponent = Vue.extend({
	router,
	name: 'DynamicLayoutDetailsDevice',
	props: ['device'],
	data: function () {
		return {
			state: {
				openState: false
			}
		}
	},
	template: `
	<li>
		<div class="arrow" @click="state.openState=!state.openState"><i :class="['fas', state.openState ? 'fa-angle-down': 'fa-angle-right']"></i></div>
		<div class="listbody">
			<span v-html="device.Icon"></span>
			{{device.DisplayName}} ({{device.Placeholders.length}})
			<ul v-if="state.openState">
				<DynamicLayoutDetailsPlaceholder v-for="placeholder in device.Placeholders" :key="placeholder.Name" :placeholder="placeholder" :device="device"></DynamicLayoutDetailsPlaceholder>
			</ul>
		</div>
	</li>
	`,
	computed: {
		id: function () { return this.device.UniqueId ? this.device.UniqueId : this.device.Id }
	},
	created: function () {
		let stateCookieString = this.$ls.get('dynamiclayoutdetails-stateCookie');
		let stateCookie = JSON.parse(stateCookieString)
		if (stateCookie) {
			this.state.openState = stateCookie.openState[this.id] ? stateCookie.openState[this.id] : false
		}
	},
	methods: {
	},
	watch: {
		'state.openState': function (newVal, oldVal) {
			let stateCookieString = this.$ls.get('dynamiclayoutdetails-stateCookie');
			let stateCookie = JSON.parse(stateCookieString)
			if (stateCookie) {
				stateCookie.openState[this.id] = this.state.openState
				this.$ls.set('dynamiclayoutdetails-stateCookie', JSON.stringify(stateCookie))
			}
		}
	}


})

Vue.component('DynamicLayoutDetailsDevice', deviceComponent)


let component = Vue.extend({
	router,
	name: 'DynamicLayoutDetails',
	data: function () {
		return {
			data: null,
			view: {
				currentLayoutType: { Name: '' }
			},
			state: {
				currentLayoutType: 'final',
				openState: {},
				editState: {},
				addRenderingState: {}
			},
			stateCookie: {}
		}
	},
	template: `
		<div class="wrapper" v-if="view.currentLayoutType!=null">
			<ul class="nav nav-tabs" v-if="data!=null">
				<li role="presentation" v-for="layoutType in data.LayoutTypes" :key="layoutType.Name" class="nav-item"><a @click="selectLayoutType(layoutType)" :class="['nav-link', { active: view.currentLayoutType.Name==layoutType.Name }]">{{layoutType.DisplayName}} Layout</a></li>
			</ul>
			<div class="dialog-page dialog-list layout-list">
				<ul>
					<DynamicLayoutDetailsDevice v-for="device in view.currentLayoutType.Devices" :key="device.Name" :device="device" />
				</ul>
			</div>
			<div class="footer">
				<button @click="saveChanges()" style="display: none"><i class="fa fa-save" title="save"></i> Save</button>
				<button @click="cancelChanges()"><i class="fa fa-trash" title="save"></i> Close</button>
				<span style="color: #fff">Current version is read only, saving will be implemented in the next version.</span>
			</div>
		</div>
	`,
	created: function () {
		let stateCookieString = this.$ls.get('dynamiclayoutdetails-stateCookie');
		let stateCookie = JSON.parse(stateCookieString)
		if (stateCookie) {
			this.state = stateCookie
		} else {
			this.$ls.set('dynamiclayoutdetails-stateCookie', JSON.stringify(this.state))
		}
		this.state.currentLayoutType = "final"; // TODO: do not hardcode this later after we add the tabs!
		this.getRenderings()
	},
	methods: {
		getRenderings: function () {

			//let query = 'id=' + this.$route.query.id +
			//	'&database=' + this.$route.query.database +
			//	'&version=' + this.$route.query.version +
			//	'&language=' + this.$route.query.language

			// note: we pass the querystring right along to the API
			this.$http.get('/mindshiftAPI/DynamicLayoutDetails/GetRenderings' + window.location.search)
				.then(response => {
					this.data = response.data
					let layoutTypeMatch = this.data.LayoutTypes.filter(layout => layout.Name === this.state.currentLayoutType)
					if (layoutTypeMatch && layoutTypeMatch.length > 0) {
						this.view.currentLayoutType = layoutTypeMatch[0]
					} else {
						this.view.currentLayoutType = this.data.LayoutTypes[0]
						this.setCurrentLayoutType(this.view.currentLayoutType)
					}

				})
		},
		setCurrentLayoutType: function (layoutType) {
			this.view.currentLayoutType = layoutType
			this.state.currentLayoutType = layoutType.Name
			//			saveState();
		},

		selectLayoutType: function (layoutType) {
			//layoutType.selected = true;
			//for (let i = 0; i < dynamicResponse.layoutTypes; i++) {
			//	dynamicResponse[i].selected = false;
			//}
			this.view.currentLayoutType = layoutType
		},
		setOpenState: function (rendering) {
			var id = rendering.UniqueId ? rendering.UniqueId : rendering.Id;
			if (!this.state.openState[id]) this.state.openState[id] = false;
			this.state.openState[id] = !this.state.openState[id];
			//saveState(); // TODO: uncomment when we're writing the cookies again!
		},
		getOpenState: function (rendering) {
			let id = rendering.UniqueId ? rendering.UniqueId : rendering.Id
			if (!this.state.openState) this.state.openState = {} // shouldn't ever be needed - just needed it because I added it later
			console.log(this.state.openState[id])
			return this.state.openState[id] // note: null or undefined is false so...
		},
		getEditState: function (rendering) { return true }
	},
	computed: {


	}
	//			var webMethod = '/DynamicPlaceholders/GetRenderings/' + $scope.view.itemId + '/' + $scope.view.database; // TODO: different path?

})

Vue.component('DynamicLayoutDetails', component)
