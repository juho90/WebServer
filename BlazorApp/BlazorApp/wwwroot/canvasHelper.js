window.canvas = {
    ctx: CanvasRenderingContext2D = null,
    initialize: function (canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            throw new Error(`Canvas with id "${canvasId}" not found.`);
        }
        this.ctx = canvas.getContext('2d');
    },
    clear: function () {
        if (!this.ctx) {
            throw new Error('Canvas context is not initialized. Call initializeCanvas first.');
        }
        const canvas = this.ctx.canvas;
        this.ctx.clearRect(0, 0, canvas.width, canvas.height);
    },
    drawCircle: function (x, y, radius, color = 'red') {
        if (!this.ctx) {
            throw new Error('Canvas context is not initialized. Call initializeCanvas first.');
        }
        this.ctx.beginPath();
        this.ctx.arc(x, y, radius, 0, 2 * Math.PI);
        this.ctx.fillStyle = color;
        this.ctx.fill();
        this.ctx.closePath();
    }
}
