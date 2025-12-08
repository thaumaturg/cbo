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
    defineRule("password_chars", (value) => {
      if (!value) return true;
      return /^[A-Za-z0-9!@#$%^&*]+$/.test(value);
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
      generateMessage: ctx => {
        const messages = {
          required: "Required",
          email: "Must be a valid email",
          min: `Must be at least ${ctx.rule?.params?.[0]} characters`,
          max: `Must be at most ${ctx.rule?.params?.[0]} characters`,
          min_value: `Must be at least ${ctx.rule?.params?.[0]}`,
          max_value: `Must be at most ${ctx.rule?.params?.[0]}`,
          username_chars: "Latin letters, numbers, and underscores",
          password_chars: "Latin letters, numbers, !@#$%^&*",
          confirmed: "Passwords do not match",
          latin_cyrillic_latvian: "Only English, Russian, and Latvian letters are allowed",
        };
        return messages[ctx.rule.name] ? messages[ctx.rule.name] : "Field is invalid";
      },
      validateOnBlur: true,
      validateOnChange: false,
      validateOnInput: false,
      validateOnModelUpdate: false,
    });
  },
};
