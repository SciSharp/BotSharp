<script>
	import Link from 'svelte-link';
	import {
		Row,
		Col,
		CardBody,
		Card,
		Container,
		Form,
		Label,
		Input,
		Button,
		Alert
	} from 'sveltestrap';
	import Headtitle from '$lib/common/HeadTitle.svelte';
	import { getToken } from '$lib/services/auth-service.js'
	import { goto } from '$app/navigation';
	import { setAuthorization } from '$lib/helpers/http';

	let username = 'guest@gmail.com';
	let password = '';
	let isOpen = false;
	let msg = '';
	let status = '';
	async function onSubmit(e) {
		e.preventDefault();

		await getToken(username, password, (token) => {
			isOpen = true;
			msg = 'Authentication success';
			status = 'success';
			setAuthorization(token);
			goto(`/chat/01fcc3e5-9af7-49e6-ad7a-a760bd12dc4a?token=${token}`);
		});
	}
</script>

<Headtitle title="Login" />
<div class="account-pages" style="margin-top: 5px;">
	<Container>
		<Row class="justify-content-center">
			<Col md={8} lg={6} xl={5}>
				<Card class="overflow-hidden">
					<div class="bg-primary-subtle">
						<Row>
							<Col class="col-7">
								<div class="text-primary p-4">
									<h5 class="text-primary">Welcome Back !</h5>
									<p>Sign in to continue live chat.</p>
								</div>
							</Col>
							<Col class="col-5 align-self-end">
								<img src='/images/profile-img.png' alt="" class="img-fluid" />
							</Col>
						</Row>
					</div>
					<CardBody class="pt-0">
						<div class="auth-logo">
							<Link href="/" class="auth-logo-light">
								<div class="avatar-md profile-user-wid mb-4">
									<span class="avatar-title rounded-circle bg-light">
										<img src='/images/logo.png' alt="" class="rounded-circle" height="55" />
									</span>
								</div>
							</Link>
							<Link href="/" class="auth-logo-dark">
								<div class="avatar-md profile-user-wid mb-4">
									<span class="avatar-title rounded-circle bg-light">
										<img src='/images/logo.png' alt="" class="rounded-circle" height="55" />
									</span>
								</div>
							</Link>
						</div>
						<div class="p-2">
							<Alert {isOpen} color={status}>{msg}</Alert>
							<Form class="form-horizontal" on:submit={onSubmit}>
								<div class="mb-3">
									<Label for="username" class="form-label">Username</Label>
									<Input
										type="text"
										class="form-control"
										id="username"
										placeholder="Enter username"
										bind:value={username}
									/>
								</div>

								<div class="mb-3">
									<Label class="form-label" for="user-password">Password</Label>
									<div class="input-group auth-pass-inputgroup">
										<Input
											type="password"
											class="form-control"
											id="user-password"
											placeholder="Enter password"
											aria-label="Password"
											aria-describedby="password-addon"
											bind:value={password}
										/>
										<Button color="light" type="button" id="password-addon"
											><i class="mdi mdi-eye-outline" /></Button
										>
									</div>
								</div>

								<div class="form-check">
									<input class="form-check-input" type="checkbox" id="remember-check" />
									<Label class="form-check-label" for="remember-check">Remember me</Label>
								</div>

								<div class="mt-3 d-grid">
									<Button color="primary" class="waves-effect waves-light" type="submit"
										>Log In</Button
									>
								</div>

								<div class="mt-4 text-center">
									<h5 class="font-size-14 mb-3">Sign in with</h5>

									<ul class="list-inline">
										<li class="list-inline-item">
											<a href={'#'} class="social-list-item bg-primary text-white border-primary">
												<i class="mdi mdi-facebook" />
											</a>
										</li>
										<li class="list-inline-item">
											<a href={'#'} class="social-list-item bg-info text-white border-info">
												<i class="mdi mdi-twitter" />
											</a>
										</li>
										<li class="list-inline-item">
											<a href={'#'} class="social-list-item bg-danger text-white border-danger">
												<i class="mdi mdi-google" />
											</a>
										</li>
									</ul>
								</div>
							</Form>
						</div>
					</CardBody>
				</Card>
			</Col>
		</Row>
	</Container>
</div>
