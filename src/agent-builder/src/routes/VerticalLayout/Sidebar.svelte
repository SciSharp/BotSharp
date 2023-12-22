<script>
	import { onMount, afterUpdate } from 'svelte';
	import 'overlayscrollbars/overlayscrollbars.css';
	import { OverlayScrollbars } from 'overlayscrollbars';
	import data from '$lib/common/data/Layoutmenudata';
	import Link from 'svelte-link';
	import { page } from '$app/stores';
	import { browser } from '$app/environment';
	import { _ } from 'svelte-i18n'

	// after routeing complete call afterUpdate function
	afterUpdate(() => {

		removeActiveDropdown()
		let currUrl = $page.url.pathname;
		if (currUrl) {
			let item = document.querySelector(".vertical-menu a[href='" + currUrl + "']");
			if (item) {
				item.classList.add('active');
				const parent1 = item.parentElement;
				if (parent1) {
					parent1.classList.add('mm-active');
					const parent2 = parent1.parentElement;
					if (parent2) {
						parent2.classList.add('mm-show');
						if (parent2.previousElementSibling) {
							parent2.previousElementSibling.classList.add('mm-active');
						}
						const parent3 = parent2.parentElement.parentElement;
						if (parent3) {
							parent3.classList.add('mm-show');
							if (parent3.previousElementSibling) {
								parent3.previousElementSibling.classList.add('mm-active');
							}
						}
					}
				}
			}
		}
	});

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

	onMount(() => {
		const menuElement = document.querySelector('#vertical-menu');
		OverlayScrollbars(menuElement, options);
		activeMenu();

		let currUrl = $page.url.pathname;
		if (currUrl) {
			let item = document.querySelector(".vertical-menu a[href='" + currUrl + "']");
			if (item) {
				item.classList.add('active');
				item.scrollIntoView({behavior: 'smooth',block: 'center'});
				const parent1 = item.parentElement;
				if (parent1) {
					parent1.classList.add('mm-active');
					const parent2 = parent1.parentElement;
					if (parent2) {
						parent2.classList.add('mm-show');
						if (parent2.previousElementSibling) {
							parent2.previousElementSibling.classList.add('mm-active');
						}
						const parent3 = parent2.parentElement.parentElement;
						if (parent3) {
							parent3.classList.add('mm-show');
							if (parent3.previousElementSibling) {
								parent3.previousElementSibling.classList.add('mm-active');
							}
						}
					}
				}
			}
		}

		// menuItemScroll()
	});

	const activeMenu = () => {
		if (browser) {
			document.querySelectorAll('.vertical-menu .has-arrow').forEach((menu) => {
				menu.addEventListener('click', () => {
					menu.classList.add('mm-active');
					if (menu.nextElementSibling) {
						menu.nextElementSibling.classList.remove('mm-collapse');
						menu.nextElementSibling.classList.add('mm-show');
					}
				});
			});

			document.querySelectorAll('.sub-menu a').forEach((submenu) => {
				submenu.addEventListener('click', () => {
					removeActiveDropdown();
					submenu.classList.add('active');
					if (submenu.nextElementSibling) {
						submenu.nextElementSibling.classList.add('mm-show');
					}
					if (submenu.parentElement) {
						submenu.parentElement.classList.add('mm-active');
						const parent1 = submenu.parentElement.parentElement;
						if (parent1) {
							parent1.classList.add('mm-show');
							if (parent1.previousElementSibling) {
								parent1.previousElementSibling.classList.add('mm-active');
							}

							const parent2 = parent1.parentElement.parentElement;
							if (parent2) {
								parent2.classList.add('mm-show');
								if (parent2.previousElementSibling) {
									parent2.previousElementSibling.classList.add('mm-active');
								}
							}
						}
					}
				});
			});
		}
	};

	const removeActiveDropdown = () => {
		document.querySelectorAll('.vertical-menu .has-arrow').forEach((menu) => {
			if (menu.nextElementSibling) {
				menu.nextElementSibling.classList.add('mm-collapse');
				menu.nextElementSibling.classList.remove('mm-show');
				menu.classList.remove('mm-active');
			}
		});

		document.querySelectorAll('.sub-menu a').forEach((submenu) => {
			submenu.classList.remove('active');
			if (submenu.parentElement) {
				submenu.parentElement.classList.remove('mm-active');
			}
		});
	};

	const menuItemScroll=() => {
		if(browser){
		let currUrl = $page.url.pathname;
			let item = document.querySelector(".vertical-menu a[href='" + currUrl + "']").offsetTop;
			if (item > 300) {
				item = item-300;
				const menuElement = document.getElementById('vertical-menu');
				menuElement.scrollTo({
					top: item,
					behavior:'smooth'
				})
			}
		}
	}
</script>

<div class="vertical-menu">
	<div class="h-100" id="vertical-menu">
		<!--- Sidemenu -->
		<div id="sidebar-menu">
			<!-- Left Menu Start -->
			<ul class="metismenu list-unstyled" id="side-menu">
				{#each data.Navdata as item}
					{#if item.isHeader}
						<li class="menu-title" key="t-menu">{$_(item.label)}</li>
					{:else if item.subMenu}
						<li>
							<Link href={null} class="has-arrow waves-effect">
								<i class={item.icon} />
								<span>{$_(item.label)}</span>
							</Link>
							<ul class="sub-menu mm-collapse">
								{#each item.subMenu as subMenu}
									{#if subMenu.isChildItem}
										<li>
											<Link href="#" class="has-arrow waves-effect">
												<span>{$_(subMenu.label)}</span>
											</Link>
											<ul class="sub-menu mm-collapse">
												{#each subMenu.childItems as childItem}
													<li><Link href={childItem.link}>{$_(childItem.label)}</Link></li>
												{/each}
											</ul>
										</li>
									{:else}
										<li><Link href={subMenu.link}>{$_(subMenu.label)}</Link></li>
									{/if}
								{/each}
							</ul>
						</li>
					{:else}
						<li>
							<Link href={item.link} class="waves-effect"
								><i class={item.icon} /> <span>{$_(item.label)}</span>
							</Link>
						</li>
					{/if}
				{/each}
			</ul>
		</div>
	</div>
</div>
