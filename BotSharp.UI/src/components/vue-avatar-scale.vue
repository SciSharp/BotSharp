<template>
	<input
          v-model="scale"
          type="range"
          :min="min"
          :max="max"
          :step="step"
          :style="{ width:width +'px'}"
        />
</template>
<script >
	export default{
		props:{
			width:{
				type:Number,
				default:200
			},
			min:{
				type:Number,
				default:1
			},
			max:{
				type:Number,
				default:2
			},
			step:{
				type:Number,
				default:0.01
			},

		},
		methods:{
			setScale:function(scale){
				this.scale = scale
			}
		},
		data:function(){
			return {
				scale:1
			}
		},
		watch:{
			scale:function(){
				this.$emit('vue-avatar-editor-scale:change-scale', this.scale)
			}
		}
	}
</script>