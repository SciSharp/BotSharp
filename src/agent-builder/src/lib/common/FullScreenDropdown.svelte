<script>
	let isFullScreenMode = true;
	import { browser } from '$app/environment';
	import Link from 'svelte-link';

	function toggleFullscreen() {
		if (browser) {
			if (
				!document.fullscreenElement &&
				/* alternative standard method */ !document.mozFullScreenElement &&
				!document.webkitFullscreenElement
			) {
				// current working methods
				isFullScreenMode = false;
				if (document.documentElement.requestFullscreen) {
					document.documentElement.requestFullscreen();
				} else if (document.documentElement.mozRequestFullScreen) {
					document.documentElement.mozRequestFullScreen();
				} else if (document.documentElement.webkitRequestFullscreen) {
					document.documentElement.webkitRequestFullscreen(Element.ALLOW_KEYBOARD_INPUT);
				}
			} else {
				isFullScreenMode = true;
				if (document.cancelFullScreen) {
					document.cancelFullScreen();
				} else if (document.mozCancelFullScreen) {
					document.mozCancelFullScreen();
				} else if (document.webkitCancelFullScreen) {
					document.webkitCancelFullScreen();
				}
			}
		}
	}
</script>

<div class="dropdown d-none d-lg-inline-block ms-1">
	<button on:click={toggleFullscreen} class="btn header-item noti-icon waves-effect">
		<i class={isFullScreenMode ? 'bx bx-fullscreen fs-22' : 'bx bx-exit-fullscreen fs-22'} />
	</button>
</div>
