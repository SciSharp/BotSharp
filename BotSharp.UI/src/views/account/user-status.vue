<template>
    <div>
        <img v-if="user.avatar" :src="user.avatar" class="user-status-img"/>
        <img v-else src="../../images/user.png" class="user-status-img"/>
        <div style="display:inline-block;float:right;"><Button type="text" @click="handleUserSettings">{{user.fullName}}</Button>
            <Button v-if="user.id" type="text" icon="android-exit" size="large" @click="handleLogout">退出</Button>
        </div>
    </div>
</template>
<script>
	
    export default {
        data(){
			return {
				
			}
		},
        computed: {
            user () {
                return this.$store.getters.user;
            }
        },
		mounted() {
			//let userId = this.$store.getters.user.id;//this.$route.query.agentId;
            this.$store.commit('refreshAgents');
		},
        methods: {
			handleLogout() {
                this.$ls.remove('token');
                this.$router.push('/');
            },
            handleUserSettings(){
                this.$router.push('/account/settings');
            }
        }
    }
</script>

<style scoped lang="less">
    .user-status-img{
        height: 32px;
    }
</style>