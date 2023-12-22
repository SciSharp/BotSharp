<script>
    import { onMount } from 'svelte';
	import Link from 'svelte-link';    

    /** @type {import('$lib/types').Pagination} */    
    export let pagination;

    /** @type {number} */
    $: totalPages = Math.ceil(pagination.count / pagination.size);
    /** @type {number} */
    $: offset = pagination.page * pagination.size;
    /** @type {number[]} */
    $: pages = Array.from(String(totalPages), Number);

    onMount(async () => {

    });    
</script>

<div class="row justify-content-between align-items-center">
    <div class="col-auto me-auto">
        <p class="text-muted mb-0">Showing <b>{offset}</b> to <b>{offset + pagination.size}</b> of <b>{pagination.count}</b> entries</p>
    </div>
    <div class="col-auto">
        <div class="card d-inline-block ms-auto mb-0">
            <div class="card-body p-2">
                <nav aria-label="Page navigation example" class="mb-0">
                    <ul class="pagination mb-0">
                        <li class="page-item">
                            <Link class="page-link" href="#" aria-label="Previous">
                                <span aria-hidden="true">&laquo;</span>
                            </Link>
                        </li>
                
                        {#each pages as page}
                            {#if page == pagination.page + 1}
                            <li class="page-item active"><Link class="page-link" href="#">{page}</Link></li>
                            {:else}
                            <li class="page-item"><Link class="page-link" href="#">{page}</Link></li>
                            {/if}
                        {/each}
                
                        <li class="page-item">
                            <Link class="page-link" href="#" aria-label="Next">
                                <span aria-hidden="true">&raquo;</span>
                            </Link>
                        </li>
                    </ul>
                </nav>                
            </div>
        </div>
    </div>
    <!--end col-->
</div>

