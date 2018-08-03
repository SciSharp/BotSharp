<style scoped lang="less">

</style>

<template>
    <Row>
        <Form ref="formInline" :model="formInline" :rules="ruleInline">
            <Form-item prop="username">
                <Input type="text" v-model="formInline.username" placeholder="Email">
                    <Icon type="ios-person-outline" slot="prepend"></Icon>
                </Input>
            </Form-item>
            <Form-item prop="password">
                <Input type="password" v-model="formInline.password" placeholder="Password">
                    <Icon type="ios-locked-outline" slot="prepend"></Icon>
                </Input>
            </Form-item>
        </Form>
        <div>
            <Button @click="handleSubmit('formInline')" style="margin-left:10px;">Login</Button>
            <p style="margin-top:30px;">Automate conversationon and enterprise processing through intelligent bot</p>
        </div>
    </Row>
</template>

<script>
    export default {
		data () {
            return {
				formInline: {
                    username: this.$config.testAccount.username,
                    password: this.$config.testAccount.password
                },
                ruleInline: {
                    username: [
                        { required: true, message: '请填写用户名', trigger: 'blur' }
                    ],
                    password: [
                        { required: true, message: '请填写密码', trigger: 'blur' },
                        { type: 'string', min: 6, message: '密码长度不能小于6位', trigger: 'blur' }
                    ]
                }
            }
        },
        methods: {
			goto(path){
				this.$router.push(path);
			},
			handleSubmit(name) {
                this.$refs[name].validate((valid) => {
                    if (valid) {
                        this.getToken();
                    } else {
                        this.$Message.error('表单验证失败!');
                    }
                })
            },
            getToken() {
                this.$ajax.post(`/token`, this.formInline, { baseURL: this.$config.baseURL })
					.then(response => {
                        this.$store.commit('authenticated', response.data)
                    });
            }
        }
    }
</script>
