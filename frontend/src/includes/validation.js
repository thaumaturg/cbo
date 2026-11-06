import { Form as VeeForm, Field as VeeField, ErrorMessage, defineRule, configure } from "vee-validate";
import { required, email, min, max, regex, confirmed, min_value, max_value } from "@vee-validate/rules";

export default {
  install(app) {
    app.component("VeeForm", VeeForm);
    app.component("VeeField", VeeField);
    app.component("ErrorMessage", ErrorMessage);

    defineRule("required", required);
    defineRule("email", email);
    defineRule("min", min);
    defineRule("max", max);
    defineRule("min_value", min_value);
    defineRule("max_value", max_value);
    defineRule("regex", regex);
    defineRule("confirmed", confirmed);
    defineRule("different", (value, [target]) => {
      if (!value) return true;
      return value !== target;
    });
    defineRule("lte_field", (value, [target]) => {
      if (value == null || target == null) return true;
      return value <= target;
    });
    defineRule("username_chars", (value) => {
      if (!value) return true;
      return /^[A-Za-z0-9_]+$/.test(value);
    });
    defineRule("latin_cyrillic_latvian", (value) => {
      if (!value) return true;
      const pattern = /^[A-Za-zĀ-žЁёА-Яа-я\s]+$/u;
      return pattern.test(value);
    });

    configure({
      generateMessage: (ctx) => {
        const label = ctx.label ?? ctx.field;
        const param = ctx.rule?.params?.[0];
        const messages = {
          required: `${label} is required.`,
          email: "Enter a valid email address.",
          min: `${label} must be at least ${param} characters long.`,
          max: `${label} must be at most ${param} characters long.`,
          min_value: `${label} must be at least ${param}.`,
          max_value: `${label} must be at most ${param}.`,
          username_chars: "Username can only contain Latin letters, numbers and underscores.",
          latin_cyrillic_latvian: `${label} can only contain Latin, Cyrillic and Latvian letters and spaces.`,
          confirmed: "Passwords don't match.",
          different: "New password must be different from the current password.",
          lte_field: `${label} can't be greater than the maximum (${param}).`,
        };
        return messages[ctx.rule?.name] ?? `${label} is invalid.`;
      },
      validateOnBlur: true,
      validateOnChange: false,
      validateOnInput: false,
      validateOnModelUpdate: false,
    });
  },
};
