<template>
    <div>
        <canvas :width="canvasWidth" :height="canvasHeight" ref="canvas" @dragover.prevent @drop="onDrop" @mousedown="onDragStart" @mouseup="onDragEnd" @mousemove="onMouseMove" @click="clicked" v-bind:class="cursor"/>
        <input type="file" id='ab-1' @change="fileSelected" style="display:none;">
   </div>
</template>
<style type="text/css">
    .cursorPointer{
        cursor: pointer;
    }
    .cursorGrab{
        cursor: grab;
        cursor: -webkit-grab;
        cursor: -moz-grab;
    }
    .cursorGrabbing{
        cursor: grabbing;
        cursor: -webkit-grabbing;
        cursor: -moz-grabbing;
    }
</style>
<script>
	const drawRoundedRect = (context, x, y, width, height, borderRadius) => {
  if (borderRadius === 0) {
    context.rect(x, y, width, height)
  } else {
    const widthMinusRad = width - borderRadius
    const heightMinusRad = height - borderRadius
    context.translate(x, y)
    context.arc(borderRadius, borderRadius, borderRadius, Math.PI, Math.PI * 1.5)
    context.lineTo(widthMinusRad, 0)
    context.arc(widthMinusRad, borderRadius, borderRadius, Math.PI * 1.5, Math.PI * 2)
    context.lineTo(width, heightMinusRad)
    context.arc(widthMinusRad, heightMinusRad, borderRadius, Math.PI * 2, Math.PI * 0.5)
    context.lineTo(borderRadius, height)
    context.arc(borderRadius, heightMinusRad, borderRadius, Math.PI * 0.5, Math.PI)
    context.translate(-x, -y)
  }
}

	export default {
		props:{
			image:{
				type:String,
				default:''
			},
			border:{
				type:Number,
				default:0
			},
			borderRadius:{
				type:Number,
				default:0
			},
			width:{
				type:Number,
				default:200
			},
			height:{
				type:Number,
				default:200
			},
			color:{
				type:Array,
				default:function(){return [0, 0, 0, 0.5]}
			}
		},
		data:function(){
			return {
                cursor:'cursorPointer',
                scale:1,
				canvas:null,
				context:null,
                dragged:false,
                imageLoaded:false,
                changed:false,
				state: {
    				drag: false,
    				my: null,
    				mx: null,
                    xxx: "ab",
    				image: {
      					x: 0,
      					y: 0,
                        resource:null
    				}
  				}
			}
		},
		computed:{
			canvasWidth:function(){
				return this.getDimensions().canvas.width
			},
			canvasHeight:function(){
				return this.getDimensions().canvas.height
			}
		},
		mounted: function(){
			this.canvas = this.$refs.canvas
			this.context = this.canvas.getContext('2d')
			this.paint()
            if(!this.image)
            {
                var placeHolder = this.svgToImage(this.context,'<svg id="Layer_1" data-name="Layer 1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 65 65"><defs><style>.cls-1{fill:#999;}</style></defs><title>Upload_Upload</title><path class="cls-1" d="M32.5,1A31.5,31.5,0,1,1,1,32.5,31.54,31.54,0,0,1,32.5,1m0-1A32.5,32.5,0,1,0,65,32.5,32.5,32.5,0,0,0,32.5,0h0Z"/><polygon class="cls-1" points="41.91 28.2 32.59 18.65 23.09 28.39 24.17 29.44 31.87 21.54 31.87 40.05 33.37 40.05 33.37 21.59 40.83 29.25 41.91 28.2"/><polygon class="cls-1" points="40.66 40.35 40.66 44.35 24.34 44.35 24.34 40.35 22.34 40.35 22.34 44.35 22.34 46.35 24.34 46.35 40.66 46.35 42.66 46.35 42.66 44.35 42.66 40.35 40.66 40.35"/></svg>');
                var self = this;
               
                placeHolder.onload = function(){
                    var dim = self.getDimensions()
                    var x = self.canvasWidth / 2 - this.width / 2
                    var y = self.canvasHeight / 2 - this.height / 2
                    self.context.drawImage(placeHolder,x,y,this.width,this.height);
                }
            }
            else
            {
                this.loadImage(this.image)
            }
		},
		methods:{
            svgToImage:function(ctx, rawSVG) {
                var svg = new Blob([rawSVG], {type:"image/svg+xml;charset=utf-8"}),
                domURL = self.URL || self.webkitURL || self,
                url = domURL.createObjectURL(svg),
                img = new Image;
                img.src = url;
                return img;
            },
			setState:function(state1){
                var min = Math.ceil(1);
                var max = Math.floor(10000);

                this.state = state1;
                this.state.cnt = 'HELLO' + Math.floor(Math.random() * (max - min)) + min
			},
			paint:function(){
				this.context.save()
		    	this.context.translate(0, 0)
		    	this.context.fillStyle = 'rgba(' + this.color.slice(0, 4).join(',') + ')'

		    	let borderRadius = this.borderRadius
		    	const dimensions = this.getDimensions()
		    	const borderSize = dimensions.border
		    	const height = dimensions.canvas.height
		    	const width = dimensions.canvas.width

		    	// clamp border radius between zero (perfect rectangle) and half the size without borders (perfect circle or "pill")
		    	borderRadius = Math.max(borderRadius, 0)
		    	borderRadius = Math.min(borderRadius, width / 2 - borderSize, height / 2 - borderSize)

		    	this.context.beginPath()
		    	// inner rect, possibly rounded
		    	drawRoundedRect(this.context, borderSize, borderSize, width - borderSize * 2, height - borderSize * 2, borderRadius)
		    	this.context.rect(width, 0, -width, height) // outer rect, drawn "counterclockwise"
		    	this.context.fill('evenodd')

		    	this.context.restore()
			},
			getDimensions:function(){
				return {
      				width: this.width,
      				height: this.height,
      				border: this.border,
      				canvas: {
        				width: this.width + (this.border * 2),
        				height: this.height + (this.border * 2)
      				}
    			}
			},
			onDrop:function(e){
				e = e || window.event
    			e.stopPropagation()
    			e.preventDefault()
				if (e.dataTransfer && e.dataTransfer.files.length) 
				{
      				//this.props.onDropFile(e)
      				const reader = new FileReader()
      				const file = e.dataTransfer.files[0]
                    this.changed = true
      				reader.onload = (e) => this.loadImage(e.target.result)
      				reader.readAsDataURL(file)
    			}
			},
			onDragStart:function(e){
				e = e || window.event
    			e.preventDefault()
                this.state.drag = true
                
                this.state.mx = null
                this.state.my = null
                this.cursor = 'cursorGrabbing'
			},
			onDragEnd: function(e){
				if (this.state.drag) {
                    this.state.drag = false
                    this.cursor = 'cursorPointer'
    			}
			},
			onMouseMove:function(e){
				e = e || window.event
    			if (this.state.drag === false) {
      				return
    			}

    			this.dragged = true
                this.changed = true

    			let imageState = this.state.image
                const lastX = imageState.x
                const lastY = imageState.y

                const mousePositionX = e.targetTouches ? e.targetTouches[0].pageX : e.clientX
                const mousePositionY = e.targetTouches ? e.targetTouches[0].pageY : e.clientY

                const newState = { mx: mousePositionX, my: mousePositionY, image: imageState }

                if (this.state.mx && this.state.my) {
                    const xDiff = (this.state.mx - mousePositionX) / this.scale
                    const yDiff = (this.state.my - mousePositionY) / this.scale

                    imageState.y = this.getBoundedY(lastY - yDiff, this.scale)
                    imageState.x = this.getBoundedX(lastX - xDiff, this.scale)
                }
                this.state.mx = newState.mx
                this.state.my = newState.my
                this.state.image = imageState
                //this.setState(newState)
			},
			loadImage: function (imageURL) {
    			var imageObj = new Image()
                var self = this
    			imageObj.onload = function(){
                    self.handleImageReady(imageObj)
                }
    			//imageObj.onerror = this.props.onLoadFailure
    			if (!this.isDataURL(imageURL)) imageObj.crossOrigin = 'anonymous'
    				imageObj.src = imageURL
  			},
  			handleImageReady: function (image) {
    			var imageState = this.getInitialSize(image.width, image.height)
    			imageState.resource = image

    			imageState.x = 0
    			imageState.y = 0

                var oldState = this.state
                oldState.drag = false
                oldState.image = imageState
                this.state.image.x = 0
                this.state.image.y = 0
                this.state.image.resource = image
                this.state.image.width = imageState.width
                this.state.image.height= imageState.height
                this.state.drag = false
                this.scale = 1
                this.$emit('vue-avatar-editor:image-ready', this.scale)
                this.imageLoaded = true
                this.cursor = 'cursorGrab'
  			},
  			getInitialSize: function (width, height) {
    			let newHeight
    			let newWidth

    			const dimensions = this.getDimensions()
    			const canvasRatio = dimensions.height / dimensions.width
    			const imageRatio = height / width

	    		if (canvasRatio > imageRatio) 
	    		{
	      			newHeight = (this.getDimensions().height)
	      			newWidth = (width * (newHeight / height))
	    		} 
	    		else 
	    		{
	      			newWidth = (this.getDimensions().width)
	      			newHeight = (height * (newWidth / width))
	    		}

	    		return {
	      			height: newHeight,
	      			width: newWidth
	    		}
	  		},
	  		isDataURL (str) {
    			if (str === null) {
      				return false
    			}
    			const regex = /^\s*data:([a-z]+\/[a-z]+(;[a-z\-]+=[a-z\-]+)?)?(;base64)?,[a-z0-9!$&',()*+;=\-._~:@\/?%\s]*\s*$/i
    			return !!str.match(regex)
  			},
  			getBoundedX:function (x, scale) {
    			var image = this.state.image
    			var dimensions = this.getDimensions()
    			let widthDiff = Math.floor((image.width - dimensions.width / scale) / 2)
    			widthDiff = Math.max(0, widthDiff)

    			return Math.max(-widthDiff, Math.min(x, widthDiff))
  			},
			getBoundedY:function (y, scale) {
    			var image = this.state.image
    			var dimensions = this.getDimensions()
    			let heightDiff = Math.floor((image.height - dimensions.height / scale) / 2)
    			heightDiff = Math.max(0, heightDiff)
				return Math.max(-heightDiff, Math.min(y, heightDiff))
  			},
            paintImage: function (context, image, border) {
                if (image.resource) {
                    var position = this.calculatePosition(image, border)
                    context.save()
                    context.globalCompositeOperation = 'destination-over'
                    context.drawImage(image.resource, position.x, position.y, position.width, position.height)
                    context.restore()
                }
            },
            calculatePosition:function (image, border) {
                var image = image || this.state.image

                var dimensions = this.getDimensions()
                var width = image.width * this.scale
                var height = image.height * this.scale

                var widthDiff = (width - dimensions.width) / 2
                var heightDiff = (height - dimensions.height) / 2
                var x = image.x * this.scale - widthDiff + border
                var y = image.y * this.scale - heightDiff + border
                return {
                    x,
                    y,
                    height,
                    width
                }
            },
            changeScale:function(sc){
                this.changed = true
                this.scale = sc
            },
            redraw:function(){
                    this.context.clearRect(0, 0, this.getDimensions().canvas.width, this.getDimensions().canvas.height)
                    this.paint()
                    this.paintImage(this.context, this.state.image, this.border)
            },
            getImage:function(){
                const cropRect = this.getCroppingRect()
                const image = this.state.image

                // get actual pixel coordinates
                cropRect.x *= image.resource.width
                cropRect.y *= image.resource.height
                cropRect.width *= image.resource.width
                cropRect.height *= image.resource.height

                // create a canvas with the correct dimensions
                const canvas = document.createElement('canvas')
                canvas.width = cropRect.width
                canvas.height = cropRect.height

                // draw the full-size image at the correct position,
                // the image gets truncated to the size of the canvas.
                canvas.getContext('2d').drawImage(image.resource, -cropRect.x, -cropRect.y)

                return canvas
            },
            getImageScaled: function(){
                const { width, height } = this.getDimensions()

                const canvas = document.createElement('canvas')
                canvas.width = width
                canvas.height = height

                // don't paint a border here, as it is the resulting image
                this.paintImage(canvas.getContext('2d'), this.state.image, 0)

                return canvas
            },
            imageChanged:function(){
                return this.changed
            },
            getCroppingRect: function () {
                const dim = this.getDimensions()
                const frameRect = { x: dim.border, y: dim.border, width: dim.width, height: dim.height }
                const imageRect = this.calculatePosition(this.state.image, dim.border)
                return {
                    x: (frameRect.x - imageRect.x) / imageRect.width,
                    y: (frameRect.y - imageRect.y) / imageRect.height,
                    width: frameRect.width / imageRect.width,
                    height: frameRect.height / imageRect.height
                }
            },
            clicked:function(e){
                if(this.dragged == true)
                {
                    this.dragged = false
                }
                else
                {
                    document.getElementById('ab-1').click()
                }
            },
            fileSelected:function(e){
                var files = e.target.files || e.dataTransfer.files;
                if (!files.length)
                    return;
                var image     = new Image();
                var reader    = new FileReader();
                this.changed = true
                reader.onload = (e) => {
                    this.loadImage(e.target.result)
                };
                reader.readAsDataURL(files[0]);
            }
		},

		watch:{
			state:{
                handler:function(val,oldval){
                    if(this.imageLoaded == true)
                        this.redraw();
                },
                deep:true
            },
            scale:function(){
                if(this.imageLoaded == true)
                    this.redraw();
            }
		}
	}


</script>