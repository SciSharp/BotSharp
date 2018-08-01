<template>
    <Tabs :animated="false">
        <Tab-pane label="基本资料" name="general">
			<Row>
				<Col span="1">&nbsp;</Col>
				<Col span="3" style="text-align:center;">
					<img v-if="user.avatar" :src="user.avatar" class="user-status-img"/>
                    <img v-else src="../../images/user.png" class="user-status-img"/>
                    <!--<Button type="ghost">上传头像</Button>-->
				</Col>
				<Col span="18">
					<Form :model="user" :label-width="80">
                        <Form-item label="注册邮箱">
							{{user.email}}
						</Form-item>
                        <Form-item label="昵称">
							<Input v-model="user.firstName" placeholder="用户昵称"></Input>
						</Form-item>
						<Form-item label="账户描述">
							<Input v-model="user.description" type="textarea" :autosize="{minRows: 2,maxRows: 5}" placeholder="账户描述..."></Input>
						</Form-item>
                        <Form-item label="创建时间">
							{{user.createdDate}}
						</Form-item>
					</Form>
				</Col>
				<Col span="2">&nbsp;</Col>
			</Row>
			<br/>
			<Row>
				<Col span="4">
					&nbsp;
				</Col>
				<Col span="20" style="text-align:center;">
					<Button type="ghost">删除</Button>
					<Button type="primary" @click="updateAgent(agent.id)">保存</Button>
				</Col>
			</Row>
		</Tab-pane>
        <Tab-pane label="安全设置" name="advance"></Tab-pane>
    </Tabs>
</template>
<script>
	
    export default {
        data(){
			return {
				user: {
					name: "Blank"
				}
			}
		},
		created() {
			let agentId = this.$route.query.agentId;
			this.$ajax.get(`/Account`, { baseURL: this.$config.authURL })
				.then(response => {
					this.user = response.data;
				});
		},
		methods:{
			updateAgent(agentId){
				this.$ajax.put('/v1/Agents/' + agentId, this.agent)
					.then(response => {
						this.$Message.info("保存成功");
					});
			}
		}
    }
</script>