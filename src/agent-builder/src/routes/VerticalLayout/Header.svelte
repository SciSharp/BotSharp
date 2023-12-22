<script>
	import { browser } from '$app/environment';
	import { Input, Dropdown, DropdownToggle, DropdownMenu, Row, Col } from 'sveltestrap';
	import Link from 'svelte-link';
	import LanguageDropdown from '$lib/common/LanguageDropdown.svelte';
	import FullScreenDropdown from '$lib/common/FullScreenDropdown.svelte';
	import NotificationDropdown from '$lib/common/NotificationDropdown.svelte';
	import ProfileDropdown from '$lib/common/ProfileDropdown.svelte';
	import { OverlayScrollbars } from 'overlayscrollbars';

	export let toggleRightBar;
	const toggleSideBar = () => {
		if (browser) {
			document.body.classList.toggle('sidebar-enable');
			document.body.classList.toggle('vertical-collpsed');

			if (document.body.classList.contains('vertical-collpsed')) {
				const Instance = OverlayScrollbars(document.querySelector('#vertical-menu'));
				if (Instance) {
					Instance.destroy();
				}
			} else {
				const options = {
					scrollbars: {
						visibility: 'auto', // You can adjust the visibility ('auto', 'hidden', 'visible')
						autoHide: 'move', // You can adjust the auto-hide behavior ('move', 'scroll', false)
						autoHideDelay: 100,
						dragScroll: true,
						clickScroll: false,
						theme: 'os-theme-light',
						pointers: ['mouse', 'touch', 'pen']
					}
				};
				const menuElement = document.querySelector('#vertical-menu');
				OverlayScrollbars(menuElement, options);
			}
		}
	};
</script>

<header id="page-topbar">
	<div class="navbar-header">
		<div class="d-flex">
			<!-- LOGO -->
			<div class="navbar-brand-box">
				<a href="/dashboard" class="logo logo-dark">
					<span class="logo-sm">
						<img src='/images/logo.png' alt="" height="25" />
					</span>
					<span class="logo-lg">
						<img src='/images/logo.png' alt="" height="65" />
					</span>
				</a>

				<a href="/dashboard" class="logo logo-light">
					<span class="logo-sm">
						<img src='/images/logo.png' alt="" height="25" />
					</span>
					<span class="logo-lg">
						<img src='/images/logo.png' alt="" height="65" />
					</span>
				</a>
			</div>

			<button
				type="button"
				class="btn btn-sm px-3 font-size-16 header-item waves-effect"
				id="vertical-menu-btn"
				on:click={toggleSideBar}
			>
				<i class="fa fa-fw fa-bars" />
			</button>

			<!-- App Search-->
			<form class="app-search d-none d-lg-block">
				<div class="position-relative">
					<Input type="text" class="form-control" placeholder="Search..." />
					<span class="bx bx-search-alt" />
				</div>
			</form>
		</div>
		<div class="d-flex">
			<LanguageDropdown />
			<FullScreenDropdown />
			<NotificationDropdown />
			<ProfileDropdown />

			<Dropdown class="d-inline-block">
				<button
					type="button"
					class="btn header-item noti-icon right-bar-toggle waves-effect"
					on:click={toggleRightBar}
				>
					<i class="bx bx-cog bx-spin" />
				</button>
			</Dropdown>
		</div>
	</div>
</header>
