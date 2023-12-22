<script>
	import { Dropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'sveltestrap';
	import { goto } from '$app/navigation';
	import { browser } from '$app/environment';

	function logout() {
		if (browser){	
			localStorage.removeItem('authUser');
		} 
		goto('/login');
	};
</script>

<Dropdown class="d-inline-block">
	<DropdownToggle
		tag="a"
		color=""
		class="btn header-item waves-effect"
		id="page-header-user-dropdown"
	>
		<img class="rounded-circle header-profile-user" src='/images/users/user-dummy.jpg' alt="Header Avatar" />
		<span class="d-none d-xl-inline-block ms-1" key="t-henry">Henry</span>
		<i class="mdi mdi-chevron-down d-none d-xl-inline-block" />
	</DropdownToggle>
	<DropdownMenu end>
		<!-- item-->
		<DropdownItem href="#"
			><i class="bx bx-user font-size-16 align-middle me-1" />
			<span>Profile</span>
		</DropdownItem>
		<DropdownItem href="#"
			><i class="bx bx-wallet font-size-16 align-middle me-1" />
			<span>My Wallet</span>
		</DropdownItem>
		<DropdownItem href="#"
			><span class="badge bg-success float-end">11</span><i
				class="bx bx-wrench font-size-16 align-middle me-1"
			/> <span key="t-settings">Settings</span>
		</DropdownItem>
		<DropdownItem href="#">
			<i class="bx bx-lock-open font-size-16 align-middle me-1" />
			<span key="t-lock-screen">Lock screen</span>
		</DropdownItem>
		<DropdownItem divider />
		<DropdownItem href="#" >
			<div on:click={logout}>
				<i class="bx bx-power-off font-size-16 align-middle me-1 text-danger" /> <span>Logout</span>
			</div>
		</DropdownItem>
	</DropdownMenu>
</Dropdown>
