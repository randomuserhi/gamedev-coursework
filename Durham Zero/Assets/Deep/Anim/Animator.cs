namespace Deep.Anim {
    public struct AnimFrame {
        public Anim anim;
        public int index;

        public AnimFrame(Anim anim, int index = 0) {
            this.anim = anim;
            this.index = index;
        }
    }
}
