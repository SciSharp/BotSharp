<script>
	import { Col, Container, Row } from "sveltestrap";

</script>

<footer class="footer">
    <Container fluid>
        <Row>
            <Col sm={6}>
                {new Date().getFullYear()} © SciSharp STACK.
            </Col>
            <Col sm={6}>
                <div class="text-sm-end d-none d-sm-block">
                    Design & Develop by open source community
                </div>
            </Col>
        </Row>
    </Container>
</footer>