<script>
	import {
		Dropdown,
		DropdownToggle,
		DropdownMenu,
		DropdownItem,
		Form,
		Input,
		Button
	} from 'sveltestrap';

	import 'overlayscrollbars/overlayscrollbars.css';
    import { OverlayScrollbars } from 'overlayscrollbars';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import Link from 'svelte-link';
	import { signalr } from '$lib/services/signalr-service.js';
    import { sendMessageToHub, GetDialogs } from '$lib/services/conversation-service.js';
	import { format } from '$lib/helpers/datetime';

	const options = {
		scrollbars: {
			visibility: 'auto',
			autoHide: 'move',
			autoHideDelay: 100,
			dragScroll: true,
			clickScroll: false,
			theme: 'os-theme-dark',
			pointers: ['mouse', 'touch', 'pen']
		}
	};
	const params = $page.params;
	
	let text = "Hi";
	
	/** @type {import('$typedefs').AgentModel} */
	export let agent;

	/** @type {import('$typedefs').UserModel} */
	export let currentUser;

	// @ts-ignore
    let scrollbar;

    /** @type {import('$typedefs').ChatResponseModel[]} */
    let dialogs = [];
	
	onMount(async () => {
		dialogs = await GetDialogs(params.conversationId);

		signalr.onMessageReceivedFromClient = onMessageReceivedFromClient;
		signalr.onMessageReceivedFromCsr = onMessageReceivedFromCsr;
		signalr.onMessageReceivedFromAssistant = onMessageReceivedFromAssistant;
		await signalr.start(params.conversationId);

		const scrollElements = document.querySelectorAll('.scrollbar');
		scrollElements.forEach((item) => {
			scrollbar = OverlayScrollbars(item, options);
		});

		refresh();
	});

	/** @param {import('$typedefs').ChatResponseModel} message */
	function onMessageReceivedFromClient(message) {
      dialogs.push(message);
      refresh();
      text = "";
    }

    /** @param {import('$typedefs').ChatResponseModel} message */
    function onMessageReceivedFromCsr(message) {
      dialogs.push(message);
      refresh();
    }

    /** @param {import('$typedefs').ChatResponseModel} message */
    function onMessageReceivedFromAssistant(message) {
      dialogs.push(message);
      refresh();
    }    

    async function sendMessage() {
      await sendMessageToHub(params.agentId, params.conversationId, text);
    }

    function refresh() {
      // trigger UI render
      dialogs = dialogs;
      setTimeout(() => {
		const { viewport } = scrollbar.elements();
		viewport.scrollTo({ top: viewport.scrollHeight, behavior: 'smooth' }); // set scroll offset
	  }, 200);
    }

	function close() {
		window.parent.postMessage({ action: "close" }, "*");
	}
</script>

<div class="d-lg-flex">
	<div class="w-100 user-chat">
		<div class="card">
			<div class="p-4 border-bottom" style="height: 12vh">
				<div class="row">
					<div class="col-md-4 col-7">
						<h5 class="font-size-15 mb-1">{agent?.name}</h5>
						<p class="text-muted mb-0">
							<i class="mdi mdi-circle text-success align-middle me-1" /> Active now
						</p>
					</div>

					<div class="col-md-8 col-5">
						<ul class="list-inline user-chat-nav text-end mb-0">
							<li class="list-inline-item">
								<Dropdown>
									<DropdownToggle tag="button" class="nav-btn dropdown-toggle" color="">
										<i class="bx bx-dots-horizontal-rounded" />
									</DropdownToggle>
									<DropdownMenu class="dropdown-menu-end">
										<DropdownItem>Action</DropdownItem>
										<DropdownItem>Another action</DropdownItem>
										<DropdownItem>Something else</DropdownItem>
									</DropdownMenu>
								</Dropdown>
							</li>
							<li class="list-inline-item d-sm-inline-block">
								<button type="submit" class="btn btn-secondary btn-rounded chat-send w-md waves-effect waves-light"
									on:click={close}
								>
									<span class="d-none d-sm-inline-block me-2" >Close</span> <i class="mdi mdi-window-close"></i>
								</button>
							</li>
						</ul>
					</div>
				</div>
			</div>

			<div class="scrollbar" style="height: 78vh">
				<div class="chat-conversation p-3">
					<ul class="list-unstyled mb-0">
						<li>
							<div class="chat-day-title">
								<span class="title">Today</span>
							</div>
						</li>
						{#each dialogs as message}
						<li id={'test_k' + message.message_id}
							class={message.sender.id === currentUser.id ? 'right' : ''}>
							<div class="conversation-list">
								<Dropdown>
									<DropdownToggle class="dropdown-toggle" tag="span" color="">
										<i class="bx bx-dots-vertical-rounded" />
									</DropdownToggle>
									<DropdownMenu class="dropdown-menu-end">
										<DropdownItem href="#">Copy</DropdownItem>
										<DropdownItem href="#">Save</DropdownItem>
										<DropdownItem href="#">Forward</DropdownItem>
										<DropdownItem href="#">Delete</DropdownItem>
									</DropdownMenu>
								</Dropdown>

								<div class="ctext-wrap">
									{#if message.sender.id === currentUser.id}
									<!--<div class="conversation-name">{message.sender.full_name}</div>-->
									<span>{message.text}</span>
									<p class="chat-time mb-0">
										<i class="bx bx-time-five align-middle me-1" />
										{format(message.created_at, 'short-time')}
									</p>									
									{:else}
									<div class="flex-shrink-0 align-self-center me-3">
										<img src="/images/users/bot.png" class="rounded-circle avatar-xs" alt="avatar">
										<span>{message.text}</span>
									</div>
									{/if}
								</div>
							</div>
						</li>
						{/each}
					</ul>
				</div>
			</div>

			<div class="p-3 chat-input-section" style="height: 10vh">
				<div class="row">
					<div class="col">
						<div class="position-relative">
							<input type="text" class="form-control chat-input" bind:value={text} placeholder="Enter Message..." />
							<div class="chat-input-links" id="tooltip-container">
								<ul class="list-inline mb-0">
									<li class="list-inline-item">
										<Link href="#" title="Add Files"><i class="mdi mdi-file-document-outline" /></Link>
									</li>
								</ul>
							</div>
						</div>
					</div>
					<div class="col-auto">
						<button
							type="submit"
							class="btn btn-primary btn-rounded chat-send w-md waves-effect waves-light"
							on:click={sendMessage}
							><span class="d-none d-sm-inline-block me-2">Send</span>
							<i class="mdi mdi-send" /></button
						>
					</div>
				</div>
			</div>
		</div>
	</div>
</div>