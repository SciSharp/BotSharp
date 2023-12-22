<script lang="ts">
	import Link from 'svelte-link/src/Link.svelte';
	import { Dropdown, DropdownToggle, DropdownMenu } from 'sveltestrap';
	import "overlayscrollbars/overlayscrollbars.css";
	import { OverlayScrollbars } from "overlayscrollbars";

	import { onMount } from 'svelte';

	const options = {
		scrollbars: {
			visibility: "auto", // You can adjust the visibility ('auto', 'hidden', 'visible')
			autoHide: "move", // You can adjust the auto-hide behavior ('move', 'scroll', false)
			autoHideDelay: 100,
			dragScroll: true,
			clickScroll: false,
			theme: "os-theme-light",
			pointers: ["mouse", "touch", "pen"],
		},
	};

	onMount(() => {
		const menuElement = document.querySelector("#notification");
		 OverlayScrollbars(menuElement, options);
	})
</script>

<Dropdown class="d-none d-lg-inline-block">
	<DropdownToggle type="button" color="" tag="a" class="btn header-item noti-icon waves-effect">
		<i class="bx bx-bell bx-tada" />
		<span class="badge bg-danger rounded-pill">3</span>
	</DropdownToggle>
	<DropdownMenu class="dropdown-menu-lg" end>
		<div class="p-3">
			<div class="row align-items-center">
				<div class="col">
					<h6 class="m-0">Notifications</h6>
				</div>
				<div class="col-auto">
					<Link href="#!" class="small">View All</Link>
				</div>
			</div>
		</div>
			<div style="max-height: 230px;" id="notification">
			<Link href="javascript: void(0);" class="text-reset notification-item">
					<div class="d-flex">
						<div class="avatar-xs me-3">
							<span class="avatar-title bg-primary rounded-circle font-size-16">
								<i class="bx bx-cart" />
							</span>
						</div>
						<div class="flex-grow-1">
							<h6 class="mb-1" >Your order is placed</h6>
							<div class="font-size-12 text-muted">
								<p class="mb-1" >If several languages coalesce the grammar</p>
								<p class="mb-0">
									<i class="mdi mdi-clock-outline" /> <span >3 min ago</span>
								</p>
							</div>
						</div>
					</div>
				</Link>
				<Link href="javascript: void(0);" class="text-reset notification-item">
					<div class="d-flex">
						<img
							src='/images/users/avatar-3.jpg'
							class="me-3 rounded-circle avatar-xs"
							alt="user-pic"
						/>
						<div class="flex-grow-1">
							<h6 class="mb-1">James Lemire</h6>
							<div class="font-size-12 text-muted">
								<p class="mb-1" >It will seem like simplified English.</p>
								<p class="mb-0">
									<i class="mdi mdi-clock-outline" /> <span >1 hours ago</span>
								</p>
							</div>
						</div>
					</div>
				</Link>
				<Link href="javascript: void(0);" class="text-reset notification-item">
					<div class="d-flex">
						<div class="avatar-xs me-3">
							<span class="avatar-title bg-success rounded-circle font-size-16">
								<i class="bx bx-badge-check" />
							</span>
						</div>
						<div class="flex-grow-1">
							<h6 class="mb-1" >Your item is shipped</h6>
							<div class="font-size-12 text-muted">
								<p class="mb-1" >If several languages coalesce the grammar</p>
								<p class="mb-0">
									<i class="mdi mdi-clock-outline" /> <span >3 min ago</span>
								</p>
							</div>
						</div>
					</div>
				</Link>

				<Link href="javascript: void(0);" class="text-reset notification-item">
					<div class="d-flex">
						<img
							src='/images/users/avatar-4.jpg'
							class="me-3 rounded-circle avatar-xs"
							alt="user-pic"
						/>
						<div class="flex-grow-1">
							<h6 class="mb-1">Salena Layfield</h6>
							<div class="font-size-12 text-muted">
								<p class="mb-1" >
									As a skeptical Cambridge friend of mine occidental.
								</p>
								<p class="mb-0">
									<i class="mdi mdi-clock-outline" /> <span >1 hours ago</span>
								</p>
							</div>
						</div>
					</div>
				</Link>
			</div>
	</DropdownMenu>
</Dropdown>
