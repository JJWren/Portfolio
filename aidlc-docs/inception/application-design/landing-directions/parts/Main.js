class Component extends DCLogic {
  constructor(props) {
    super(props);
    this.state = { theme: null };
  }
  renderVals() {
    var theme = this.state.theme || this.props.theme || 'dark';
    var raw = Number(this.props.degrees);
    var degrees = Number.isFinite(raw) ? Math.max(0, Math.min(6, Math.round(raw))) : 0;
    var self = this;
    return {
      theme: theme,
      on: true,
      beltRed: this.props.beltRed || '#a63d40',
      stripes: Array.from({ length: degrees }, function (_, i) { return { i: i }; }),
      toggleTheme: function () { self.setState({ theme: theme === 'dark' ? 'light' : 'dark' }); },
      toggleTitle: theme === 'dark' ? 'Switch to the white gi (light theme)' : 'Switch to the black belt (dark theme)'
    };
  }
}
