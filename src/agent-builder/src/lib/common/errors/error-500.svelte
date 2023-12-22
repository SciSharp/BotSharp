<script>
	import Link from 'svelte-link';
	import { Col, Container, Row } from 'sveltestrap';
	import HeadTitle from '$lib/common/HeadTitle.svelte';
</script>

<HeadTitle title="500 Error Page" />

<div class="account-pages my-5 pt-5">
	<Container>
	  <Row>
		<Col lg="12">
		  <div class="text-center mb-5">
			<h1 class="display-2 fw-medium">
			  5
			  <i class="bx bx-buoy bx-spin text-primary display-3" />
			  0
			</h1>
			<h4 class="text-uppercase">Internal Server Error</h4>
			<div class="mt-5 text-center">
			  <Link class="btn btn-primary" href="/dashboard">
				Back to Dashboard
			  </Link>
			</div>
		  </div>
		</Col>
	  </Row>
	  <Row class="justify-content-center">
		<Col md="8" xl="6">
		  <div>
			<img src='/images/error-img.png' alt="" class="img-fluid" />
		  </div>
		</Col>
	  </Row>
	</Container>
  </div>


