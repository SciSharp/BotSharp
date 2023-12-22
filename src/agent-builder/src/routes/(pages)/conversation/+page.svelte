<script>
	import {
		Button,
		Card,
		CardBody,
		Col,
		Dropdown,
		DropdownItem,
		DropdownMenu,
		DropdownToggle,
		Input,
		Row,
		Table
	} from 'sveltestrap';
	import Breadcrumb from '$lib/common/Breadcrumb.svelte';
	import HeadTitle from '$lib/common/HeadTitle.svelte';
	import Pagination from '$lib/common/Pager.svelte';
	import { onMount } from 'svelte';
	import Link from 'svelte-link';
    import { getConversations, deleteConversation } from '$lib/services/conversation-service.js';
	import { format } from '$lib/helpers/datetime';

    /** @type {import('$types').PagedItems<import('$types').ConversationModel>} */
    let conversations = {count: 0, items: []};	

    /** @type {import('$types').ConversationFilter} */
    let filter = { 
		pager: { page: 0, size: 20, count: 0 }, 
		agentId: '01fcc3e5-9af7-49e6-ad7a-a760bd12dc4a', 
		channel: null, 		
		status: null 
	};	

	/** @type {import('$types').Pagination} */
	let pager = filter.pager;	

    onMount(async () => {
		await getConversationList();
    });

    function refreshPager() {
        pager = {
			page: filter.pager.page,
			size: filter.pager.size,
			count: conversations.count
		}
    }

	async function getConversationList() {
		conversations = await getConversations(filter);

		refreshPager();
	}

    /** @param {string} conversationId */
    async function handleConversationView(conversationId) {

    }

    /** @param {string} conversationId */
    async function handleConversationDeletion(conversationId) {
        await deleteConversation(conversationId);
        conversations.count--;
        conversations.items = conversations.items.filter(x => x.id != conversationId);
        refreshPager();
    }
</script>

<HeadTitle title="Conversation List" />

<Breadcrumb title="Communication" pagetitle="Conversations" />

<Row>
	<Col lg="12">
		<Card>
			<CardBody class="border-bottom">
				<div class="d-flex align-items-center">
					<h5 class="mb-0 card-title flex-grow-1">Conversation List</h5>
					<div class="flex-shrink-0">
						<Link href="#" class="btn btn-light" on:click={getConversationList}><i class="mdi mdi-magnify" /></Link>
						<Dropdown class="dropdown d-inline-block">
							<DropdownToggle type="menu" class="btn" id="dropdownMenuButton1">
								<i class="mdi mdi-dots-vertical" /></DropdownToggle
							>
							<DropdownMenu>
								<DropdownItem href="#">Action</DropdownItem>
								<DropdownItem href="#">Another action</DropdownItem>
								<DropdownItem href="#">Something else here</DropdownItem>
							</DropdownMenu>
						</Dropdown>
					</div>
				</div>
			</CardBody>
			<CardBody class="border-bottom">
				<Row class="g-3">
					<Col xxl="4" lg="6">
						<Input
							type="search"
							class="form-control"
							id="searchTableList"
							placeholder="Search for ..."
						/>
					</Col>
					<Col xxl="2" lg="6">
						<select class="form-select" id="idStatus" bind:value={filter.status}>
							<option value={null}>Status</option>
							<option value="open">Active</option>
							<option value="closed">Completed</option>
						</select>
					</Col>
					<Col xxl="2" lg="4">
						<select class="form-select" id="idType" bind:value={filter.channel}>
							<option value={null}>Select Channel</option>
							<option value="webchat">Live Chat</option>
							<option value="phone">Phone</option>
                            <option value="email">Email</option>
						</select>
					</Col>
					<Col xxl="2" lg="4">
						<Input type="date" class="form-control" />
					</Col>
					<Col xxl="2" lg="4">
						<Button type="button" color="secondary" class="btn-soft-secondary w-100">
							<i class="mdi mdi-filter-outline align-middle" /> Filter
						</Button>
					</Col>
				</Row>
			</CardBody>
			<CardBody>
				<div class="table-responsive">
					<Table class="align-middle nowrap" bordered>
						<thead>
							<tr>
								<th scope="col">Title</th>
								<th scope="col">Customer Name</th>
								<th scope="col">Role</th>
								<th scope="col">Channel</th>
								<th scope="col">Posted Date</th>
								<th scope="col">Last Date</th>
								<th scope="col">Status</th>
								<th scope="col">Action</th>
							</tr>
						</thead>
						<tbody>
							{#each conversations.items as conv}
							<tr>
								<td scope="row">
									<a href={`/communication/${conv.id}`}>{conv.title}</a></td>
								<td>{conv.user.full_name}</td>
								<td>{conv.user.role}</td>
								<td><span class="badge badge-soft-success">{conv.channel}</span></td>
								<td>{format(conv.created_time, 'time')}</td>
								<td>{format(conv.updated_time, 'time')}</td>
								<td><span class="badge bg-success">{conv.status}</span></td>
								<td>
									<ul class="list-unstyled hstack gap-1 mb-0">
										<li data-bs-toggle="tooltip" data-bs-placement="top" title="View Dialog">
											<Link href="#" on:click={() => handleConversationView(conv.id)} class="btn btn-sm btn-soft-primary">
                                                <i class="mdi mdi-eye-outline" />
                                            </Link>
										</li>
										<li data-bs-toggle="tooltip" data-bs-placement="top" title="Edit">
											<Link href="{`/chat/${conv.agent_id}/${conv.id}`}" target="_blank" class="btn btn-sm btn-soft-info">
                                                <i class="mdi mdi-pencil-outline" />
                                            </Link>
										</li>
										<li data-bs-toggle="tooltip" data-bs-placement="top" title="Delete">
											<Link href="#" on:click={() => handleConversationDeletion(conv.id)} class="btn btn-sm btn-soft-danger">
                                                <i class="mdi mdi-delete-outline" />
                                            </Link>
										</li>
									</ul>
								</td>
							</tr>
							{/each}
						</tbody>
					</Table>
				</div>

				<Pagination pagination={pager} />

			</CardBody>
		</Card>
	</Col>
</Row>
