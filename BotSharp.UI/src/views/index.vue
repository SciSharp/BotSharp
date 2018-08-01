<template>
    <div class="index">
        <Row type="flex" justify="center" align="middle">
			<Col span="24">
				<Row type="flex" justify="center">
					<Col span="2"></Col>
					<Col span="20">
						<div></div>
						<h1>
							<img src="../images/logo.png">
						</h1>
						<div class="title">The Chatbot Platform Builder for Enterprise</div>
						<div class="slogan"><vue-typer text='Hello！' :type-delay='500' :repeat='0'></vue-typer></div>
						
					</Col>
					<Col span="2" style="height: 95%">
						
					</Col>
				</Row>
				<Row type="flex" justify="center" align="middle">
					<Col>
						<login></login>
					</Col>
				</Row>
				<Row style="height:100px;" type="flex" justify="center" align="bottom">
					<Col>
						<span>© 2018 The BotSharp</span>
					</Col>
				</Row>
			</Col>

        </Row>
    </div>
</template>

<script>
	import {VueTyper} from 'vue-typer';
	import login from './account/login.vue';
	
    export default {
		data () {
            return {
            }
        },
        methods: {
			goto(path){
				this.$router.push(path);
			}
        },
		components: {
			VueTyper,
			login
		}
    }
</script>

<style scoped lang="less">
    .index{
        width: 100%;
        position: absolute;
        top: 0;
        bottom: 0;
        left: 0;
        text-align: center;
        h1{
            height: 200px;
            img{
                height: 100%;
            }
        }
        h2{
            color: #666;
            margin-bottom: 200px;
            p{
                margin: 0 0 50px;
            }
        }
        .ivu-row-flex{
            height: 100%;
        }
    }
	.title{
		font-size:2em;
		margin-top:1.5em;
	}
	.slogan{
		font-size:1.2em;
		margin-bottom:3em;
	}
</style>