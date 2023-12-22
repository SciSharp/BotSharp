<script>
    import {browser} from "$app/environment"
    import Link from "svelte-link";
    import { Container, Row, Col } from "sveltestrap";
    import HeadTitle from "$lib/common/HeadTitle.svelte";

    // import Countdown from "svelte-countdown";
    const countdown = () => {
      if(browser){
      // Set the date we're counting down to
      var countDownDate = new Date("Dec 30, 2024").getTime();
  
      // Update the count down every 1 second
      var countDown = setInterval(function () {
        // Get today's date and time
        var currentTime = new Date().getTime();
  
        // Find the distance between currentTime and the count down date
        var distance = countDownDate - currentTime;
  
        // Time calculations for days, hours, minutes and seconds
        var days = Math.floor(distance / (1000 * 60 * 60 * 24));
        var hours = Math.floor(
          (distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60)
        );
        var minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        var seconds = Math.floor((distance % (1000 * 60)) / 1000);
  
        var countDownBlock =
          '<div class="coming-box">' +
          days +
          "<span>Days</span></div>" +
          '<div class="coming-box">' +
          hours +
          "<span>Hours</span>" +
          "</div>" +
          '<div class="coming-box">' +
          minutes +
          "<span>Minutes</span>" +
          "</div>" +
          '<div class="coming-box">' +
          seconds +
          "<span>Seconds</span>" +
          "</div>";
  
        // Output the result in an element with id="countDownBlock"
        document.getElementById("countdown").innerHTML = countDownBlock;
  
        // If the count down is over, write some text
        if (distance < 0) {
          clearInterval(countDown);
          document.getElementById("countdown").innerHTML =
            '<div class="countdown-endtxt">The countdown has ended!</div>';
        }
      }, 1000);
    }
    };
      countdown();
  </script>
  
  <HeadTitle title="Coming Soon" />
  
  <div class="home-btn d-none d-sm-block">
    <Link href="/" class="text-dark">              
        <img
            src="/images/logo.png"
            alt="logo"
            height="50"
            class="auth-logo-dark mx-auto"
        />
    </Link>
  </div>
  
  <div class="my-5 pt-sm-5">
      <Container>
          <Row>
              <Col lg="12">
                  <div class="text-center">
                      <Row class="justify-content-center mt-5">
                          <Col sm="8">
                              <div class="maintenance-img">
                                <img src="/images/megamenu-img.png" alt="" style="width: 50vw;" />
                              </div>
                          </Col>
                      </Row>
                      <h4 class="mt-5">Let&#39;s <a href="/login">get started</a> with Agent Builder</h4>
                      <p class="text-muted">
                          It will be as simple as Occidental in fact it will be Occidental.
                      </p>
  
                      <Row class="justify-content-center mt-5">
                          <Col md="8">
                              <div id="countdown" class="counter-number" />
                          </Col>
                      </Row>
                  </div>
              </Col>
          </Row>
      </Container>
  </div>
  