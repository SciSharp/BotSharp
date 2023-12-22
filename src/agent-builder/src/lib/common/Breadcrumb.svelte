<script>
	import { Row, Col, Breadcrumb, BreadcrumbItem } from 'sveltestrap';
	import Link from 'svelte-link';

	export let title;
	export let pagetitle;
</script>

<Row>
	<Col xs="12">
		<div class="page-title-box d-sm-flex align-items-center justify-content-between">
			<h4 class="mb-sm-0 font-size-18">{pagetitle}</h4>
			<div class="page-title-right">
				<ol class="breadcrumb m-0">
					<BreadcrumbItem>
						<Link href="#">{title}</Link>
					</BreadcrumbItem>
					<BreadcrumbItem active>
						{pagetitle}
					</BreadcrumbItem>
				</ol>
			</div>
		</div>
	</Col>
</Row>
